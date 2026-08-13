# Source and Research Policy

## 1. Source Priority

Use sources in this order:

1. Repository code, tests, configuration, and generated artifacts.
2. Official specifications and vendor documentation.
3. Maintainer-authored design documents.
4. Reputable technical references.
5. Community examples and discussions.

Use community sources to discover ideas, not as the sole authority for critical claims.

## 2. Claim Classification

Classify material while drafting:

```text
Verified fact
Repository observation
External sourced claim
Engineering judgment
Assumption
Open question
```

Do not present engineering judgment as a sourced fact.

## 3. Citation Rules

- Link the source nearest to the relevant claim.
- Prefer primary sources.
- Include access or version context when facts may change.
- Do not cite a search-result summary when the original page is available.
- Quote sparingly; paraphrase the idea and preserve attribution.
- Never reproduce another Skill wholesale.

## 4. Borrowing From Existing Skills

It is acceptable to adapt general workflow patterns such as:

- Explicit trigger conditions.
- Ordered execution phases.
- Content-type routing.
- Templates and checklists.
- Source-of-truth discovery.
- Validation gates.
- Discoverability and index maintenance.

Do not copy distinctive prose, large examples, or licensed assets without permission.

## 5. Sources Consulted for knowledge-architect

### Agent Skills specification

Source: [Agent Skills Specification](https://agentskills.io/specification)

Adapted principles:

- Required `name` and `description` metadata.
- Description states both capability and invocation conditions.
- Progressive disclosure through `SKILL.md`, `references/`, `scripts/`, and `assets/`.
- One-level relative references.
- Deterministic validation.

### Agent Skills engineering guidance

Source: [Equipping agents for the real world with Agent Skills](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills)

Adapted principles:

- Keep core instructions focused.
- Move detailed, conditional material into references.
- Use scripts for repeatable deterministic tasks.
- Observe real usage and refine the Skill.

### Knowledge capture workflow

Source: marketplace trial Skill `knowledge-capture`, loaded during design.

Adapted principles:

- Extract concepts, decisions, procedures, and examples.
- Classify content before choosing a destination.
- Prefer updating existing knowledge when appropriate.
- Make every page discoverable through indexes and links.

No Notion-specific tools or text were copied into this Skill.

### Architecture Decision Record workflow

Source: [ADR Author Skill](https://github.com/JayRHa/AgentSkills/tree/main/adr-author)

Adapted principles:

- Separate facts from assumptions.
- Compare genuinely viable options.
- State negative consequences.
- Tie decisions to explicit drivers.

### API documentation workflow

Source: [API Docs Writer Skill](https://github.com/JayRHa/AgentSkills/tree/main/api-docs-writer)

Adapted principles:

- Locate the source of truth before writing.
- Never invent missing behavior.
- Use consistent structures and realistic examples.
- End with deterministic validation and self-review.

### Agent instruction research

Source: [How to write a great agents.md](https://github.blog/ai-and-ml/github-copilot/how-to-write-a-great-agents-md-lessons-from-over-2500-repositories/)

Adapted principles:

- State exact responsibilities and boundaries.
- Include executable validation commands.
- Prefer concrete output examples over vague prose.

## 6. Research Completion

Research is sufficient when:

- The primary question is answerable.
- Material disagreements are identified.
- Claims have appropriate evidence.
- Additional searching is unlikely to change the recommendation.

Stop before collecting redundant sources that do not improve confidence.

