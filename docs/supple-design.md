# Supple Design in RewardFlow

This guide interprets patterns in existing code. Chapter 10 is the reference; C# examples, layers and guardrails are local choices. The [official book contents](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217) lists six patterns and discusses declarative design. [Eric Evans' DDD Reference](https://www.domainlanguage.com/ddd/reference/) complements the book.

## Learning path

Read RewardPoints and its tests, then RewardsWallet, before following credit into PostgreSQL and HTTP. Ask which business difficulty each choice makes easier to understand and change.

| Pattern | Evidence | Observable example | Limit |
| --- | --- | --- | --- |
| Intention-Revealing Interfaces | Credit, Debit, CanAfford, Suspend and RewardReason | Read calls without implementation; OpenCreditDebitAndReadCoordinateTheAggregate | Public contracts do not require a C# interface for every class |
| Side-Effect-Free Functions | Immutable arithmetic; CanAfford without mutation | ArithmeticReturnsNewValuesAndPreservesOperands; CanAffordIsPureAndIncludesTheExactBalance | CanAfford reads receiver state; database queries depend on I/O and reading time |
| Assertions | Valid construction, balance, overflow and transitions | ArithmeticRejectsOverflowAndNegativeResults; RejectedOperationsDoNotChangeBalance; LifecycleStateGovernsWalletBehavior | Not merely a utility or Debug.Assert; rules remain active in Release |
| Conceptual Contours | Points calculate, wallets govern, transactions record | Types, ADR-001 and DuplicateTransactionRollsBackBalanceUpdate | Folders/layers alone do not establish business contours |
| Standalone Classes | Value objects work without wallets, HTTP or storage | Construct RewardPoints in domain tests | Small assertion collaborations remain; standalone does not mean zero dependencies |
| Closure of Operations | Add/Subtract accept and return RewardPoints | Compose 100, add 25, subtract 40, yielding 85 | Subtraction is partial; addition is bounded by long; invalid operations fail |

See [Domain](../src/RewardFlow.SuppleDesign.Domain), [DomainTests.cs](../test/RewardFlow.SuppleDesign.Tests/DomainTests.cs) and [ApplicationTests.cs](../test/RewardFlow.SuppleDesign.Tests/ApplicationTests.cs).

## End-to-end example

An active wallet opens at zero. Crediting 100 and debiting 40 leaves 60. CanAfford(60) returns true without saving; debiting 61 fails and preserves balance. Suspension permits credits, blocks debits and makes affordability false, including for zero.

Application constructs valid values and invokes the aggregate. Persistence saves balance and evidence together using the loaded revision. Matching operationId, wallet, direction, points and normalized reason replays the original result, even after lifecycle changes; it is not a new movement.

## Architectural support and limitations

Value Object, Entity, Aggregate and Repository support the examples. Clean Architecture, adapters, EF Core and idempotency help execute/protect the model but do not establish Supple Design by their presence.

IRewardsWallet has one implementation and is an explicit teaching contract. Concrete names also express intention. RewardTransaction has operational identity but C# record structural equality; explain these separately.

Declarative design has no dedicated lesson. Do not claim complete Chapter 10 coverage or add a DSL/Specifications without a concrete domain need.

## Adding a lesson

The [transfer implementation](transfer-plan.md) adds TransferAmount (1..100), RewardTransferCalculator and atomic paired ledger persistence. [ADR-003](decisions/003-atomic-wallet-transfers.md) explains the domain service and transaction trade-offs. TransferDomainTests demonstrates calculations without mutation; TransferPostgresTests verifies rollback and revision conflicts, TransferApiTests exercises HTTP, and TransferMigrationTests verifies historical replay and downgrade protection.

Describe valid input, rejection and state effects. Choose the rule owner, add behavior tests and implement the smallest revealing model change. Explain the relevant pattern and its limits. Follow [guardrails](guardrails.md) and the English [language constitution](constitution.md).
