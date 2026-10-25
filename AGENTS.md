# RewardFlow contributor instructions

The purpose is to teach Chapter 10 of Eric Evans' DDD through objective, executable C# examples. Model clarity comes first; architecture and adapters support the lesson.

## Before changing files

Read the relevant README section, [constitution](docs/constitution.md), [pattern guide](docs/supple-design.md), [guardrails](docs/guardrails.md) and applicable ADRs.

- For business rules, value objects or use cases, use [.agents/skills/rewardflow-evolve/SKILL.md](.agents/skills/rewardflow-evolve/SKILL.md).
- For design reviews or educational accuracy, use [.agents/skills/rewardflow-review/SKILL.md](.agents/skills/rewardflow-review/SKILL.md).
- Small wording fixes do not require both skills. If discovery is unavailable, read the linked skill directly.

## Essential rules

- **Write documents, plans, ADRs, skills, diagram labels, code, identifiers and code comments in English.** README.md is canonical; README.pt-BR.md is the explicit translation exception. Follow the constitution.
- Preserve invariants, immutable RewardPoints arithmetic and queries without mutation. HTTP and EF Core stay in adapters; Domain and Application remain free of third-party packages.
- Preserve detached snapshots, optimistic revisions, global operationId idempotency and atomic balance/ledger writes. Keep ledger history outside wallet reconstitution (ADR-001).
- Justify abstractions with concrete rules/boundaries. Do not add mediators, generic repositories, event buses or services merely to display patterns.
- Intention-Revealing Interfaces does not require a C# interface. Database reads are not pure calculations. Closure of Operations does not make arithmetic total or unbounded.
- Explain educational benefits without attributing local C# choices to Evans.
- Align shared claims in both READMEs. Distinguish implementation, test evidence and proposals.

## Verification

For runtime changes: run `docker compose up -d --wait`, then `dotnet test RewardFlow.SuppleDesign.slnx --configuration Release`. PostgreSQL is required for persistence and HTTP tests. For documentation only, review links, examples, language and the diff.

Report changes, educational motivation, checks and limitations. Instructions guide work; guardrails distinguish review criteria from automated checks.
