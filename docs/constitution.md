# Project constitution

## Purpose

RewardFlow teaches Eric Evans' Supple Design through objective, small and executable reward-wallet examples. Business meaning and educational clarity guide decisions. Infrastructure supports the lesson.

## Language

**English is the canonical language of this repository.** Write documentation, plans, ADRs, skills, agent instructions, diagram labels, code, identifiers, code comments, migration names, examples and commit/PR descriptions in English. Contributor conversations may use their preferred language without changing repository language.

README.md is canonical. README.pt-BR.md is the explicitly maintained Portuguese translation and the current exception to the documentation language rule. Align its shared technical claims with the English version; keep code snippets and identifiers in English. Do not introduce additional translations unless explicitly requested.

Translate existing non-English supporting documentation when updating it, preserving meaning, technical facts and links. Translation must not introduce runtime changes or turn a proposal into implemented behavior.

## Design and evidence

Keep domain invariants and immutable point arithmetic in Domain, orchestration in Application, and HTTP/EF Core in adapters. Justify abstractions with concrete rules or boundaries. Distinguish Evans' concepts from local implementation choices.

Preserve detached snapshots, optimistic revisions, global idempotency and atomic balance/ledger persistence. The [transfer plan](transfer-plan.md) proposes extending these guarantees to two wallets; it is not implemented.

Separate implemented behavior, executed tests and proposed acceptance criteria. Label proposed diagrams. Generate executable migrations with implementation, not during documentation planning.

## Applying this constitution

Read [AGENTS.md](../AGENTS.md), the relevant skill and [guardrails](guardrails.md). Instructions guide work; CI verifies only implemented checks. State and justify exceptions explicitly.

