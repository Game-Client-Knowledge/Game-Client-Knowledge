---
name: "knowledge-architect"
description: "Creates systematic explanations and maintains navigable Markdown knowledge bases. Invoke for technical concepts, architecture analysis, design trade-offs, or documentation updates."
---

# Knowledge Architect

## Purpose

Turn substantive questions into two coordinated deliverables:

1. A concise, high-signal chat summary.
2. A systematic Markdown knowledge base that remains clear, connected, and searchable as it grows.

This skill treats documentation as an information architecture, not an append-only transcript.

## When To Use

Invoke for:

- Systematic explanations of technical or domain concepts.
- Software architecture, data flow, runtime, storage, scheduling, or performance analysis.
- Design reviews, alternatives, trade-offs, and missing-assumption analysis.
- Multi-part questions that should become durable documentation.
- Follow-up questions that extend an existing documentation topic.
- Requests to organize, restructure, index, or maintain a knowledge base.

Do not invoke for trivial one-line answers, simple translations, or transient status updates unless the user explicitly requests documentation.

## Required Reading

Before acting, read:

1. `references/workflow.md`
2. `references/information-architecture.md`
3. `references/quality-gates.md`

Then read `references/content-patterns.md` for the content type selected during classification.

Read `references/source-policy.md` when external research, citations, or uncertain claims are involved.

Use `assets/topic-readme-template.md` when creating a topic index and `assets/architecture-analysis-template.md` when starting a substantial architecture review. Adapt templates to the task; do not preserve empty sections.

When modifying this Skill, test it against `references/evaluation-cases.md`.

## Non-Negotiable Rules

1. Write substantive results under `docs/`.
2. Give each topic a dedicated directory and `README.md`.
3. Prefer updating an existing relevant document over creating a near-duplicate.
4. Separate facts, assumptions, recommendations, and unresolved questions.
5. Explain mechanisms and causality, not only conclusions.
6. Use realistic examples and show important failure modes.
7. State trade-offs and applicability boundaries for architecture recommendations.
8. Never invent repository behavior, API contracts, measurements, or source claims.
9. End with a concise chat summary and clickable file links.
10. Complete the Global Recomposition Gate before every final response.

## Strict Workflow

Follow these phases in order.

### Phase 1: Discover

1. Read workspace rules and relevant memory.
2. List all Markdown files under `docs/`.
3. Read `docs/README.md` if it exists.
4. Read the relevant topic `README.md` and related documents.
5. Search headings, keywords, links, and terminology across all documentation.
6. Inspect source code or primary evidence when the answer depends on project behavior.

### Phase 2: Classify

Classify the request as one or more of:

- Concept explanation
- Mechanism deep dive
- Architecture analysis
- Decision/trade-off analysis
- How-to procedure
- Reference documentation
- FAQ/follow-up clarification
- Knowledge-base restructuring

Use the matching pattern from `references/content-patterns.md`.

### Phase 3: Model

Create a small knowledge map before writing:

```text
Question
-> Direct answer
-> Prerequisite concepts
-> Mechanism / causal chain
-> Concrete example
-> Trade-offs and limits
-> Failure modes
-> Related topics
```

For architecture work, additionally model:

```text
Goals
-> Constraints and assumptions
-> Components and responsibilities
-> Data/control flow
-> Runtime and failure behavior
-> Alternatives
-> Consequences
-> Verification
```

### Phase 4: Research

1. Prefer repository code, specifications, and official documentation.
2. Search external sources when knowledge is current, disputed, or explicitly requested.
3. Record source URLs and distinguish sourced facts from engineering judgment.
4. Do not copy another Skill verbatim. Extract reusable workflow principles and write original instructions.
5. Mark uncertainty instead of filling gaps with plausible details.

### Phase 5: Compose

1. Decide whether to update, split, merge, rename, or create documents.
2. Write the direct answer early.
3. Progress from overview to mechanism, example, trade-offs, and verification.
4. Use tables for comparisons, diagrams for relationships, and code for executable ideas.
5. Keep one primary subject per file.
6. Add cross-links where a reader naturally needs prerequisite or follow-up material.
7. Update the topic `README.md` reading order.

### Phase 6: Global Recomposition Gate

This phase is mandatory after every substantive answer, including follow-up edits.

1. Rescan every Markdown file under `docs/`, not only changed files.
2. Review the full H1/H2 outline and all relative links.
3. Reassess whether the current directory and chapter boundaries still match the knowledge structure.
4. Merge duplicated explanations or split files with multiple unrelated responsibilities.
5. Reorder chapters when prerequisite order has changed.
6. Update `docs/README.md`, every affected topic `README.md`, and previous/next navigation.
7. Add missing prerequisite, related-topic, and continuation links.
8. Remove or reconnect orphan documents.
9. Normalize terminology and resolve contradictory statements.
10. Preserve stable paths when possible; if renaming is necessary, update all inbound links.

Do not treat this as a link-only check. It is a semantic information-architecture review.

### Phase 7: Validate

Run:

```bash
python3 .trae/skills/knowledge-architect/scripts/audit_docs.py docs
```

Then verify:

- One H1 per document.
- No skipped heading levels.
- Balanced code fences.
- No broken relative links.
- Every document is reachable from `docs/README.md`.
- Every topic directory has a `README.md`.
- Examples and code agree with the explanation.
- Claims are sourced or clearly labeled as judgment.
- The answer addresses every part of the user request.

Fix all errors before finalizing. Review warnings and either fix them or explain why they are intentional.

### Phase 8: Respond

The final chat response should contain:

1. The direct conclusion in a short paragraph.
2. Links to the main documents or changed sections.
3. Verification status.
4. Any material uncertainty or limitation.

Do not repeat the full document in chat.

## Documentation Layout

Default layout:

```text
docs/
├── README.md
└── <topic>/
    ├── README.md
    ├── 01-<foundation>.md
    ├── 02-<mechanism>.md
    ├── 03-<implementation>.md
    └── ...
```

Use descriptive topic directories and stable kebab-case file names. Number files only when a clear reading sequence exists.

## Quality Standard

A strong result lets a reader answer:

- What is it?
- Why does it exist?
- How does it work?
- What assumptions does it make?
- What is a concrete example?
- What are the alternatives and trade-offs?
- When should it not be used?
- How can the claim or implementation be verified?
- Where should the reader go next?

## Boundaries

- Do not create documentation that merely restates code without explaining intent.
- Do not create a new file for every follow-up question.
- Do not preserve a poor existing hierarchy solely to minimize edits.
- Do not reorder or rename files without repairing navigation.
- Do not use diagrams as decoration; every diagram must clarify a relationship or flow.
- Do not optimize for document count. Optimize for comprehension and retrieval.
