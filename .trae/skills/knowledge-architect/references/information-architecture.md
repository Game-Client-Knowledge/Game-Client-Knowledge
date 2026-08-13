# Information Architecture Reference

## 1. Design Goals

The documentation hierarchy must optimize for:

```text
Discovery
Comprehension
Progressive learning
Direct lookup
Maintenance
Stable linking
```

Do not optimize for chronological creation order.

## 2. Three Navigation Layers

### Root index

`docs/README.md` answers:

- What topics exist?
- What does each topic cover?
- Which entry should a reader choose?
- Are there cross-topic learning paths?

### Topic index

`docs/<topic>/README.md` answers:

- What is the topic scope?
- What is explicitly out of scope?
- What is the short answer?
- What is the recommended reading order?
- Which page serves which reader goal?

### Chapter navigation

Each sequential chapter should link to:

- Previous chapter.
- Topic index.
- Next chapter.

Add semantic cross-links in the body when a prerequisite or related mechanism is needed.

## 3. Page Types

Use page types deliberately:

| Page type | Reader intent |
|---|---|
| Overview | Understand scope and mental model |
| Concept | Learn what something means |
| Mechanism | Understand how and why it works |
| Architecture | Understand components and relationships |
| How-to | Complete a procedure |
| Reference | Look up precise facts |
| Decision | Understand a choice and consequences |
| FAQ | Resolve a narrow recurring question |
| Troubleshooting | Diagnose symptoms and causes |

Do not mix page types without a clear reason.

## 4. Naming

Titles should match natural search intent:

```text
Good:
Component Storage and Query Implementation
High-Performance ECS Storage
Scheduler DAG Design Analysis

Weak:
More Details
Advanced Notes
Miscellaneous
Part Two
```

File names should be stable, descriptive, and kebab-case.

Use numeric prefixes only for a real learning sequence:

```text
01-fundamentals.md
02-core-model.md
03-runtime-flow.md
```

Do not use numbers merely to preserve creation order.

## 5. Granularity

A page should have one primary reader question.

Split when:

- The H1 needs “and” to join unrelated concepts.
- Independent sections have different audiences.
- The page contains multiple distinct search intents.
- A section is referenced frequently from other pages.

Merge when:

- Pages cannot be distinguished in the topic index.
- Pages repeat more context than unique content.
- Each page is only a fragment of one explanation.

## 6. Cross-Link Semantics

Every link should communicate why it exists:

```markdown
For generation-based stale handle detection, see
[Entity lifecycle validation](./component-query.md#generation-validation).
```

Avoid bare “click here” links.

Useful link relationships:

- Prerequisite
- Detailed implementation
- Alternative design
- Trade-off analysis
- Previous/next learning step
- Source or evidence

## 7. Recomposition Algorithm

After every substantive update:

### Inventory

Collect:

```text
Path
H1
H2 outline
Inbound links
Outbound links
Topic directory
Sequence prefix
```

### Diagnose

Look for:

- Orphan pages.
- Broken links.
- Competing titles.
- Duplicate explanations.
- Missing prerequisite links.
- Invalid sequence order.
- Topic directories without indexes.
- Pages that no longer match their topic.
- Contradictory terminology.
- Oversized or fragmented pages.

### Recompose

Apply the smallest set of structural changes that restores clarity:

```text
Update index descriptions
Reorder reading sequence
Add or remove cross-links
Merge duplicates
Split mixed-purpose pages
Move pages to the correct topic
Rename only when search intent materially improves
```

### Verify

Start at `docs/README.md` and mentally navigate to every changed page. Then run the audit script to verify mechanical integrity.

## 8. Index Entry Quality

An index entry must explain the page distinction:

```markdown
1. [Core model](./02-core-model.md)
   Defines Entity, Component, System, World, Query, and storage ownership.
2. [Runtime flow](./03-runtime-flow.md)
   Explains scheduling, visibility, events, and structural change boundaries.
```

A list of filenames without descriptions is not a useful information architecture.

## 9. Stability Versus Improvement

Preserve paths when:

- Existing links are likely used externally.
- A title is imperfect but unambiguous.
- Reordering can be expressed in the index without renaming.

Change paths when:

- The page is materially misclassified.
- Duplicate pages are merged.
- The filename actively prevents discovery.

When changing a path, update every repository reference in the same change.

