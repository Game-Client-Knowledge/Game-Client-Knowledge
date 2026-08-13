# Evaluation Cases

Use these cases when changing the Skill.

## Case 1: Follow-Up Clarification

Prompt:

```text
The existing document says an Entity has a generation.
How is generation compared if every Entity has a different value?
```

Expected behavior:

- Update the existing Entity lifecycle section.
- Explain per-slot comparison with concrete values.
- Do not create a disconnected new topic.
- Rescan all documents and repair navigation if line structure changes.
- Return a concise summary with a section link.

## Case 2: Architecture Critique

Prompt:

```text
Analyze a scheduler that builds a DAG from System inputs and outputs,
prunes it from player input each frame, then runs Systems serially.
```

Expected behavior:

- Classify as architecture analysis.
- Separate valid ideas, incorrect assumptions, and missing semantics.
- Cover state, side effects, dependency types, concurrency, and time boundaries.
- Give a corrected architecture and trade-offs.
- Use the architecture template as a starting point.

## Case 3: Performance Mechanism

Prompt:

```text
Why avoid hash lookups and unpredictable branches in an ECS hot loop?
```

Expected behavior:

- Explain causality from memory access to Cache Miss and CPU stalls.
- Distinguish contiguous allocation from contiguous access.
- Explain when small hash tables are acceptable.
- Cover branch prediction without claiming all branches are bad.
- Include a before/after loop example and measurement guidance.

## Case 4: New Topic

Prompt:

```text
Explain event sourcing systematically with architecture and examples.
```

Expected behavior:

- Create a dedicated topic directory and topic `README.md`.
- Establish a reading order before producing many chapters.
- Cover model, event store, write/read flow, consistency, snapshots, trade-offs, and examples.
- Add the topic to `docs/README.md`.
- Ensure every page is reachable from the root index.

## Case 5: Knowledge-Base Recomposition

Prompt:

```text
The documentation has grown confusing. Reorganize it.
```

Expected behavior:

- Inventory all documents and outlines.
- Identify duplicate search intent, orphans, and incorrect sequence.
- Propose and execute a move/merge/split map.
- Preserve content and repair all links.
- Run the audit script.
- Summarize structural changes rather than repeating document content.

## Failure Conditions

The Skill fails evaluation if it:

- Answers only in chat without updating requested documentation.
- Appends a page without checking existing related content.
- Treats link validation as a substitute for semantic recomposition.
- Produces a new file that is unreachable from `docs/README.md`.
- Gives architecture advice without assumptions or trade-offs.
- Invents source behavior or external facts.
- Ends before validation completes.

