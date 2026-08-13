# Workflow Reference

## 1. Intake

Extract:

```text
Primary question
Subquestions
Expected depth
Target audience
Requested output format
Existing topic or new topic
Evidence needed
```

Do not ask for clarification when a conservative assumption is sufficient. State the assumption in the document.

## 2. Documentation Discovery

Use fast repository-wide discovery:

```bash
rg --files docs -g '*.md' | sort
rg -n '^#{1,2} ' docs
rg -n '<keyword>|<synonym>' docs
```

Read:

1. Root documentation index.
2. Relevant topic index.
3. Documents matching the concept and its prerequisites.
4. Adjacent chapters referenced by previous/next links.

The goal is to understand the current knowledge graph before changing it.

## 3. Update, Create, Split, or Merge

### Update an existing file when

- The question clarifies an existing section.
- The new content shares the same reader goal.
- The content is short enough to remain scannable.
- Creating a new page would duplicate context.

### Create a new file when

- The content has an independent search intent.
- It requires multiple sections, examples, or trade-offs.
- Multiple existing pages should link to it.
- It introduces a new architectural concern.

### Split a file when

- It answers multiple unrelated reader questions.
- Its heading outline no longer has a single coherent purpose.
- Readers must scroll through unrelated material to find the answer.
- The file mixes tutorial, reference, and decision history without clear boundaries.

### Merge files when

- They repeat the same prerequisites or conclusions.
- Each file is too small to justify independent discovery.
- Their titles compete for the same search intent.
- Navigation cannot explain their distinction in one sentence.

## 4. Knowledge Modeling

Before drafting, write a private outline:

```text
Direct answer
Terms that require definitions
Causal mechanism
Example with concrete values
Counterexample or failure mode
Trade-offs
Applicability boundary
Related documents
```

For architecture:

```text
Problem and goals
Constraints and assumptions
Current/proposed components
Ownership boundaries
Data flow
Control flow
State and lifecycle
Concurrency and failure behavior
Operational concerns
Alternatives
Decision and consequences
```

## 5. Drafting Order

Draft in this order:

1. Direct conclusion.
2. Scope and terminology.
3. Mechanism.
4. Concrete example.
5. Alternatives and trade-offs.
6. Failure modes.
7. Verification.
8. Related links.

Readers should receive the answer before the background.

## 6. Architecture Analysis Discipline

Separate:

| Category | Treatment |
|---|---|
| Fact | Cite code, measurement, specification, or source |
| Assumption | Label and explain what changes if false |
| Constraint | State whether fixed or negotiable |
| Recommendation | Tie to goals and constraints |
| Risk | Describe likelihood, impact, and mitigation |
| Open question | Identify owner or evidence needed |

For a proposed design, include at least one serious alternative. Do not create straw-man options.

## 7. Global Recomposition

After writing:

1. Generate the complete document inventory.
2. Review all H1/H2 headings as one global outline.
3. Verify topic boundaries and reading order.
4. Find duplicated terms, repeated explanations, and competing titles.
5. Check whether new content changes prerequisite order.
6. Repair root index, topic indexes, chapter navigation, and cross-links.
7. Confirm every file is reachable from the root documentation index.
8. Run the deterministic audit script.

Global recomposition is complete only when a new reader can:

- Discover the topic from `docs/README.md`.
- Understand the topic index without opening every file.
- Follow a sensible prerequisite order.
- Find the new answer by title, heading, or related link.
- Continue to the next relevant subject.

## 8. Delivery

The chat response is intentionally smaller than the artifact:

```text
Conclusion
Main file links
What was reorganized
Validation result
```

Do not paste the whole document into chat.

