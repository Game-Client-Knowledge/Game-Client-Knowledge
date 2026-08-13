# Content Patterns

Choose the smallest pattern that fully serves the reader.

## 1. Concept Explanation

```text
Title
1. Direct definition
2. Problem it solves
3. Core properties
4. How it works
5. Concrete example
6. Common misunderstandings
7. Applicability boundary
8. Related topics
```

Use for “what is X” and foundational knowledge.

## 2. Mechanism Deep Dive

```text
Title
1. Short answer
2. Inputs and state
3. Step-by-step causal chain
4. Data structures or runtime model
5. Worked example with values
6. Edge cases
7. Cost model
8. Verification
```

Use for “how does X actually work” and performance questions.

## 3. Architecture Analysis

```text
Title
1. Executive conclusion
2. Proposed/current architecture
3. Goals
4. Constraints and assumptions
5. Components and ownership
6. Data and control flow
7. Strengths
8. Defects, risks, and missing semantics
9. Alternatives
10. Recommended architecture
11. Migration or implementation sequence
12. Verification metrics
```

For each issue, explain:

```text
What is wrong
Why it is wrong
When it matters
Concrete failure scenario
Correction
Trade-off introduced by the correction
```

## 4. Decision and Trade-Off Analysis

```text
Title
1. Decision statement
2. Context
3. Facts and assumptions
4. Decision drivers
5. Viable options
6. Comparison matrix
7. Recommendation
8. Positive consequences
9. Negative consequences
10. Risks and follow-up actions
```

Include at least two genuinely viable options. “Do nothing” is valid when realistic.

## 5. How-To Guide

```text
Title
1. Outcome
2. Prerequisites
3. Steps
4. Expected result
5. Verification
6. Troubleshooting
7. Rollback or cleanup
8. Related reference
```

Commands must be executable and state their working directory or prerequisites.

## 6. Reference Documentation

```text
Title
1. Scope
2. Stable terminology
3. Exact structures or contracts
4. Parameter/field tables
5. Examples
6. Errors and edge cases
7. Compatibility/version notes
8. Source of truth
```

Do not invent unspecified fields or behavior.

## 7. FAQ or Follow-Up Clarification

Prefer updating the relevant concept or mechanism section.

Use:

```text
Question as heading
Short answer
Detailed explanation
Concrete example
Why the confusion occurs
Related section
```

Create a standalone FAQ page only when multiple narrow questions form a durable collection.

## 8. Knowledge-Base Restructuring

```text
1. Inventory
2. Current problems
3. Proposed taxonomy
4. Move/merge/split map
5. Navigation updates
6. Link repair
7. Validation
8. Change summary
```

Avoid content loss. Read both source and destination before merging.

## 9. Diagram Selection

| Need | Diagram |
|---|---|
| Components and ownership | Flowchart or container diagram |
| Request or event sequence | Sequence diagram |
| Lifecycle | State diagram |
| Dependency ordering | DAG |
| Data transformation | Data-flow diagram |
| Alternatives | Comparison table before diagram |

Every diagram must be followed by a short textual interpretation.

## 10. Example Quality

A useful example has:

- Concrete names.
- Realistic values.
- Initial state.
- Operation.
- Result.
- Explanation of why the result occurs.

Avoid examples that merely rename variables from the definition.

