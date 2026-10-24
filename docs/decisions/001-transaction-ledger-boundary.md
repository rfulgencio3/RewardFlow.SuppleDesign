# ADR-001: Transaction ledger boundary

- Status: Accepted
- Date: 2026-10-23

## Context

A balance alone cannot explain why points changed, and retrying an HTTP request must not apply the same business operation twice. Loading an ever-growing history into `RewardsWallet` would also make every command depend on data it does not need to protect the current balance.

## Decision

`RewardsWallet` remains the aggregate root responsible for balance and lifecycle rules. `RewardTransaction` is immutable evidence created after the aggregate accepts a credit or debit. It has the client-provided `operationId`, wallet identity, movement type, points, reason, UTC instant and resulting balance.

The repository saves the wallet revision and transaction atomically. `operationId` is globally unique and acts as the idempotency identity. An identical retry returns the persisted result; reusing the ID with different data is a conflict. The ledger is queried separately and never reconstituted as part of the wallet aggregate.

## Consequences

- Commands load only the state needed to enforce wallet invariants.
- The database guarantees that a balance change and its audit record commit together.
- Callers must retain an operation ID across retries.
- The application coordinates replay semantics while the domain types express the transaction vocabulary and validity.
