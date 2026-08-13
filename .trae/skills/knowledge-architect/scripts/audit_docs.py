#!/usr/bin/env python3
"""Audit a Markdown documentation tree for structure and discoverability."""

from __future__ import annotations

import argparse
import re
import sys
from collections import defaultdict, deque
from dataclasses import dataclass, field
from pathlib import Path
from urllib.parse import unquote, urlsplit


HEADING_RE = re.compile(r"^(#{1,6})\s+(.+?)\s*$")
LINK_RE = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
FENCE_RE = re.compile(r"^\s*(`{3,}|~{3,})")
EXTERNAL_SCHEMES = {
    "data",
    "file",
    "ftp",
    "http",
    "https",
    "mailto",
    "tel",
}


@dataclass
class Document:
    path: Path
    relative: Path
    headings: list[tuple[int, str, int]] = field(default_factory=list)
    links: list[tuple[str, int]] = field(default_factory=list)
    fence_open: tuple[str, int] | None = None

    @property
    def title(self) -> str:
        h1 = [text for level, text, _ in self.headings if level == 1]
        return h1[0] if len(h1) == 1 else "<invalid H1>"


def parse_document(path: Path, root: Path) -> Document:
    document = Document(path=path, relative=path.relative_to(root))
    fence_char: str | None = None
    fence_length = 0
    fence_line = 0

    for line_number, line in enumerate(
        path.read_text(encoding="utf-8").splitlines(),
        start=1,
    ):
        fence = FENCE_RE.match(line)
        if fence:
            marker = fence.group(1)
            marker_char = marker[0]

            if fence_char is None:
                fence_char = marker_char
                fence_length = len(marker)
                fence_line = line_number
            elif marker_char == fence_char and len(marker) >= fence_length:
                fence_char = None
                fence_length = 0
                fence_line = 0
            continue

        if fence_char is not None:
            continue

        heading = HEADING_RE.match(line)
        if heading:
            document.headings.append(
                (len(heading.group(1)), heading.group(2), line_number)
            )

        for match in LINK_RE.finditer(line):
            target = match.group(1).strip()
            if target.startswith("<") and target.endswith(">"):
                target = target[1:-1]
            document.links.append((target, line_number))

    if fence_char is not None:
        document.fence_open = (fence_char * fence_length, fence_line)

    return document


def local_target(
    source: Path,
    raw_target: str,
    root: Path,
) -> Path | None:
    target = raw_target.split(maxsplit=1)[0]
    parsed = urlsplit(target)

    if parsed.scheme.lower() in EXTERNAL_SCHEMES or parsed.netloc:
        return None
    if not parsed.path:
        return None

    decoded = unquote(parsed.path)
    candidate = (source.parent / decoded).resolve()

    try:
        candidate.relative_to(root)
    except ValueError:
        return candidate

    if candidate.is_dir():
        candidate = candidate / "README.md"

    return candidate


def audit(root: Path) -> int:
    errors: list[str] = []
    warnings: list[str] = []

    if not root.exists() or not root.is_dir():
        print(f"ERROR: documentation root does not exist: {root}")
        return 1

    root = root.resolve()
    paths = sorted(root.rglob("*.md"))

    if not paths:
        print(f"ERROR: no Markdown files found under {root}")
        return 1

    documents = {
        path.resolve(): parse_document(path.resolve(), root)
        for path in paths
    }
    graph: dict[Path, set[Path]] = defaultdict(set)
    incoming: dict[Path, int] = defaultdict(int)

    for path, document in documents.items():
        h1 = [
            (text, line)
            for level, text, line in document.headings
            if level == 1
        ]
        if len(h1) != 1:
            errors.append(
                f"{document.relative}: expected exactly one H1, found {len(h1)}"
            )

        previous_level = 0
        for level, text, line in document.headings:
            if previous_level and level > previous_level + 1:
                errors.append(
                    f"{document.relative}:{line}: heading level jumps "
                    f"from H{previous_level} to H{level} ({text})"
                )
            previous_level = level

        if document.fence_open is not None:
            marker, line = document.fence_open
            errors.append(
                f"{document.relative}:{line}: unclosed code fence {marker}"
            )

        for raw_target, line in document.links:
            target = local_target(path, raw_target, root)
            if target is None:
                continue
            if not target.exists():
                errors.append(
                    f"{document.relative}:{line}: broken link -> {raw_target}"
                )
                continue
            if target.suffix.lower() == ".md" and target in documents:
                graph[path].add(target)

    for source, targets in graph.items():
        for target in targets:
            if source != target:
                incoming[target] += 1

    root_index = (root / "README.md").resolve()
    if root_index not in documents:
        errors.append("README.md: root documentation index is missing")
    else:
        reachable: set[Path] = set()
        queue: deque[Path] = deque([root_index])

        while queue:
            current = queue.popleft()
            if current in reachable:
                continue
            reachable.add(current)
            queue.extend(graph.get(current, set()) - reachable)

        for path, document in documents.items():
            if path not in reachable:
                errors.append(
                    f"{document.relative}: not reachable from README.md"
                )

    top_level_topics = {
        document.relative.parts[0]
        for document in documents.values()
        if len(document.relative.parts) > 1
    }
    for topic in sorted(top_level_topics):
        topic_index = (root / topic / "README.md").resolve()
        if topic_index not in documents:
            errors.append(f"{topic}/README.md: topic index is missing")

    for path, document in documents.items():
        if path != root_index and incoming[path] == 0:
            warnings.append(f"{document.relative}: has no inbound Markdown link")

    print(f"Documentation root: {root}")
    print(f"Documents scanned: {len(documents)}")
    print()
    print("Inventory:")
    for document in sorted(
        documents.values(),
        key=lambda item: str(item.relative),
    ):
        h2_count = sum(
            1 for level, _, _ in document.headings if level == 2
        )
        print(
            f"- {document.relative}: {document.title} "
            f"(H2={h2_count}, inbound={incoming[document.path]})"
        )

    print()
    print(f"Errors: {len(errors)}")
    for error in errors:
        print(f"ERROR: {error}")

    print(f"Warnings: {len(warnings)}")
    for warning in warnings:
        print(f"WARNING: {warning}")

    return 1 if errors else 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Audit Markdown headings, fences, links, topic indexes, "
            "and reachability."
        )
    )
    parser.add_argument(
        "root",
        nargs="?",
        default="docs",
        type=Path,
        help="documentation root (default: docs)",
    )
    args = parser.parse_args()
    return audit(args.root)


if __name__ == "__main__":
    sys.exit(main())

