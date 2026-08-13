# Quality Gates

Complete every applicable gate before finalizing.

## 1. Coverage Gate

- [ ] Every user question is answered.
- [ ] The direct conclusion appears early.
- [ ] Required prerequisite concepts are defined.
- [ ] Mechanisms include causal explanation.
- [ ] At least one concrete example is included when useful.
- [ ] Architecture advice includes trade-offs and boundaries.
- [ ] Unknowns and assumptions are explicit.

## 2. Evidence Gate

- [ ] Repository behavior is grounded in code or runtime evidence.
- [ ] External facts use reliable sources.
- [ ] Current or time-sensitive claims were verified.
- [ ] Recommendations are distinguishable from sourced facts.
- [ ] No API, metric, field, or behavior was invented.
- [ ] Conflicting sources are acknowledged.

## 3. Structure Gate

- [ ] One H1 exists per file.
- [ ] Heading levels do not jump.
- [ ] Sections progress from general to specific.
- [ ] Paragraphs are short and focused.
- [ ] Tables are used for real comparisons.
- [ ] Diagrams clarify a relationship or process.
- [ ] Code blocks use language identifiers where practical.

## 4. Information Architecture Gate

- [ ] All Markdown files were rescanned after the edit.
- [ ] Root index describes every topic.
- [ ] Topic index describes page scope and reading order.
- [ ] New content updates prerequisite and continuation links.
- [ ] No orphan document exists.
- [ ] No duplicate page competes for the same search intent.
- [ ] Previous/next navigation matches current order.
- [ ] Terminology is consistent across documents.
- [ ] Moved or renamed files have no stale inbound links.

## 5. Example Gate

- [ ] Example initial state is clear.
- [ ] Inputs and operations are concrete.
- [ ] Output is shown.
- [ ] The explanation connects output to mechanism.
- [ ] Edge cases are included when they affect correctness.
- [ ] Example code is internally consistent.

## 6. Architecture Gate

- [ ] Goals and constraints are explicit.
- [ ] Ownership boundaries are clear.
- [ ] Data flow and control flow are distinguished.
- [ ] State lifecycle is covered.
- [ ] Concurrency and failure semantics are covered when relevant.
- [ ] At least one viable alternative is considered.
- [ ] Negative consequences are stated.
- [ ] Verification metrics or tests are proposed.

## 7. Mechanical Gate

Run:

```bash
python3 .trae/skills/knowledge-architect/scripts/audit_docs.py docs
```

The command must report zero errors.

Warnings require review. Fix discoverability warnings unless the file is intentionally excluded and that exclusion is documented.

## 8. Chat Delivery Gate

- [ ] Chat summary is materially shorter than the document.
- [ ] Main artifacts use clickable file links.
- [ ] Verification status is reported.
- [ ] Limitations are reported.
- [ ] The response does not end with a generic offer.

