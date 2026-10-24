---
name: rewardflow-evolve
description: Evolve RewardFlow business rules, value objects and use cases while preserving its educational Supple Design examples. Use for model behavior changes, not isolated wording fixes.
---

# Evolve the RewardFlow model

Read root AGENTS.md, docs/constitution.md and docs/guardrails.md. Use docs/supple-design.md to distinguish patterns from local C# adaptations.

Write documents, plans, ADRs, skills, diagram labels, code and code comments in English. README.md is canonical; README.pt-BR.md is the explicit translation exception. Conversation language does not change repository language.

Express the rule with inputs, expected results and a rejected case. Identify invariant ownership: points calculate, wallets govern balance/lifecycle, Application coordinates, adapters persist. Read affected use cases and tests.

Make the smallest change exposing the concept. Justify new abstractions with a concrete need and learning cost. Do not add frameworks merely to expand the demonstration.

Test behavior at the layer deciding the rule. Movement changes require state preservation on rejection. Persistence changes require PostgreSQL evidence for revisions, idempotency and atomicity. Use explicit clocks. For transfers, consult docs/transfer-plan.md and ADR-003; they remain proposals until implemented.

Update affected comments and shared README claims. Add an ADR for decisions spanning types. Report before/after behavior, educational benefit, checks and remaining limits.
