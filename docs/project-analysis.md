# Project analysis recorded on 2026-10-24

## Scope and result

The review examined four production projects, ports, use cases, persistence mappings, startup, HTTP contracts, tests, READMEs, ADRs, build, Compose and CI. `dotnet test RewardFlow.SuppleDesign.slnx --configuration Release --verbosity minimal` restored, built and completed **38 passed, zero failed and zero skipped**, using SDK 9.0.308 and healthy local PostgreSQL. This is historical evidence, not exhaustive certification.

The six guide patterns are demonstrated well. Main improvements concern precision and links between claims, code and tests. The original scope did not justify more architecture.

## Model and boundaries

RewardPoints owns arithmetic, equality and bounds. RewardsWallet owns balance/lifecycle with private setters and validation before assignment. WalletName/RewardReason expose normalization. Dates enter Domain explicitly; Application uses TimeProvider.

Use cases coordinate load, behavior and persistence without controller balance rules. Results avoid exposing mutable aggregates. Domain has no references/packages; Application and Infrastructure reference Domain; Api composes both. EF Core/ASP.NET stay outside the core.

PostgreSQL reconstructs isolated snapshots, updates by revision and saves movements transactionally. Global IDs identify operations; ordering uses timestamp/ID. Wallet loads omit ledger history. ADR-002 explains startup migrations.

## Strong evidence

| Test | Conclusion |
| --- | --- |
| ArithmeticReturnsNewValuesAndPreservesOperands | New values preserve operands |
| RejectedOperationsDoNotChangeBalance | Zero, insufficient balance and overflow preserve state |
| LifecycleStateGovernsWalletBehavior | Suspension permits credit, blocks debit; closure is terminal |
| RepeatedOperationIsIdempotentAndAppearsOnceInTheLedger | Sequential replay and changed-payload conflict |
| StaleSnapshotCannotOverwritePersistedBalance | Real adapter rejects stale writes |
| DuplicateTransactionRollsBackBalanceUpdate | Ledger conflict rolls back balance |
| ConcurrentIdenticalCreditsConvergeOnOneMovement | Concurrent real credits converge in the exercised scenario |
| CreditRetryIsIdempotentAndLedgerIsPaged | HTTP replay and hasNextPage |

See [tests](../test/RewardFlow.SuppleDesign.Tests).

## Findings

### Educational precision — high

C# interfaces are a valid teaching device, but concrete public contracts also reveal intention. Technical layers alone are not Conceptual Contours.

ListRewardTransactions performs I/O despite a Side-Effect-Free Function comment. Describe a query without mutation; RewardPoints is a stronger calculation example. CanAfford depends on receiver state. The [guide](supple-design.md) clarifies these limits.

Declarative design has no dedicated lesson. Declare that limit rather than adding a DSL to fill a checklist.

### Original README accuracy — high

The original English README incorrectly described movement responses as points-only and suspended affordability queries as rejected. Movement responses use balanceAfter; affordability returns false. The preceding documentation delivery corrected these claims; this record preserves the diagnosis.

The .http file uses fixed global IDs. Reusing them with another wallet can return 409. Guidance now calls for fresh IDs for new operations and reuse only for retries.

### Tests — medium

InvalidPointsReturnBadRequest omits operationId/reason; those missing fields can mask point-validation regressions. Keep other fields valid.

The original README overstated restart and no-COUNT test coverage. No dedicated tests establish those claims. Storage supports durability; pageSize+1 avoids counting in implementation. READMEs now distinguish implementation from tests.

Dedicated reason-bound, non-UTC, invalid-enum, debit-replay and cross-wallet/direction ID cases remain absent. Do not claim exhaustive coverage.

### Contracts/concurrency — medium

ListRewardTransactions relies on controller pagination bounds. Direct callers can cause overflow or meaningless pages. Make Application preconditions explicit before another adapter.

The double locks movement writes but not all status writes/reads; it does not prove complete concurrent isolation. Use PostgreSQL for critical cases.

Task.WhenAll in the concurrent credit test does not force both callers to load the same revision. The test supports its exercised scenario, not every race window.

### Identity/cross-object invariants — low

RewardTransaction has business identity but structural record equality. Explain the distinction before entity collection use.

The save port takes wallet/transaction separately without adapter checks for matching WalletId/BalanceAfter. Current use cases construct matching pairs; the signature alone does not prevent inconsistent callers.

## Follow-up planning

The requested [transfer plan](transfer-plan.md) introduces a concrete need for a two-wallet calculation and wider transaction, recorded in [proposed ADR-003](decisions/003-atomic-wallet-transfers.md). Interpret 100 points per transfer and require source funding. The 38 historical tests do not establish implemented transfers.

This delivery changes guidance/planning only. Runtime findings remain future increments. The [constitution](constitution.md) establishes English documentation with README.pt-BR.md as the translation exception.
