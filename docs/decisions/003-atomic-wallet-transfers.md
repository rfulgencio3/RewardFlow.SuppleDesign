# ADR-003: Atomic wallet transfers

- Status: Proposed — planning only, not implemented
- Date: 2026-10-24
- Extends: [ADR-001](001-transaction-ledger-boundary.md)

## Context

A transfer removes 1..100 points from an active source wallet and credits a different eligible destination. The source must have sufficient balance. Calling the existing debit and credit use cases would commit independently and allow partial transfers. The current ledger uses operation_id as its primary key and cannot record both sides under one identity.

## Proposed decision

Keep wallets as separate aggregates. Introduce a small TransferAmount value object and a domain calculator that uses shared wallet debit/credit previews to produce immutable resulting balances without I/O or mutation. The application coordinates snapshots, replay and persistence.

Use one PostgreSQL transaction to reserve a globally unique operation ID, conditionally update both wallet revisions in stable ID order and insert transfer evidence plus two ledger entries. Introduce reward_operations, separate ledger transaction_id from operation_id, and backfill existing credit/debit operations. All movement endpoints share the operation namespace and replay rules.

Source suspension blocks transfers out; destination suspension permits transfers in, consistently with current credit rules. The 100-point limit is per transfer, not daily. These are explicit planning assumptions.

## Alternatives and consequences

- Separate debit/credit commits are rejected because compensation is not the requested atomic behavior.
- A larger aggregate containing all wallets would hide independent lifecycles and create unnecessary contention.
- Saga/eventual consistency is unnecessary for a single database and would require pending states and recovery rules outside this lesson.
- One transaction across aggregates is a deliberate local trade-off: stronger immediate consistency with contention and a shared store. It is not a universal DDD recommendation.
- A shared operation registry adds schema and adapter work, justified by global idempotency across single and paired ledger entries. No generic workflow engine is introduced.
- SQL constraints do not enforce all paired-entry invariants; the specific save contract and PostgreSQL integration tests must exercise them.

See the [implementation plan](../transfer-plan.md) for HTTP, diagrams, schema, migration, concurrency cases and acceptance criteria. ADR-001 remains accepted for the running system until this proposal is implemented. ADR-002's startup migration policy remains unchanged; no executable migration is included in the planning change.
