# Implementation and review guardrails

These criteria guide people and agents. Tests verify exercised behavior; educational quality and conceptual accuracy require review.

## Purpose, language and scope

Follow the [constitution](constitution.md): documents, plans, ADRs, skills, diagram labels, code and comments are English. README.pt-BR.md is the explicit translation exception.

Keep the model explainable through reward examples. Features need concrete rules and educational benefit. Transfers are implemented as described in the [design and verification guide](transfer-plan.md). Expiration, campaigns and authentication remain future possibilities. Infrastructure must be proportional to the lesson.

## Review criteria

| Guardrail | Location | Evidence when affected |
| --- | --- | --- |
| Whole points from zero to long.MaxValue; positive movements | RewardPoints, RewardsWallet | Bounds, rejection preserving state, exact-balance redemption |
| Valid normalized names/reasons, nonempty IDs, UTC instants | Constructors, values, use cases | Valid/invalid core examples |
| Wallet owns lifecycle | RewardsWallet | Initial/final states, permissions, terminal closure |
| Queries do not save/create wallets | Query use cases | Results and unchanged persistence; distinguish I/O from pure calculation |
| Domain has no project dependencies; Application depends on Domain; core has no external packages | csproj/imports | Diff review; build alone does not enforce architecture |
| HTTP validates format/translates errors; domain protects rules | Contracts, controller, handler | HTTP tests with other fields valid and direct rule tests |
| Atomic balance/evidence | Repository, ADR-001 | PostgreSQL rollback tests |
| Revision checks and detached snapshots | Port/adapters | Stale-write rejection and no storage leakage |
| Global operationId and original-result replay | Use cases/repository | Replay, payload conflict and concurrency |
| Ledger outside reconstitution | Repository | Bounded/deterministic pagination, wallet loading without history |

Use TrySaveMovementAsync for application credit/debit paths, not TrySaveAsync. The double synchronizes every read/write on one gate for atomic visibility, but cannot prove PostgreSQL atomicity.

Transfers additionally require domain limits 1..100, distinct wallets, sufficient source balance, lifecycle/overflow checks, calculation without mutation, both revisions and paired entries in one transaction, cross-endpoint ID handling and history-preserving migration. Do not compose independently committing debit/credit cases. The transfer tests exercise the domain, HTTP, atomicity, revision/replay and upgrade paths. Separate operation and entry identities as accepted in ADR-003; do not assume exhaustive concurrency coverage.

## Abstractions and explanations

Identify a concrete rule/boundary before adding types, interfaces, services or frameworks. Hypothetical reuse and pattern names do not justify layers. Reconsider abstractions obscuring vocabulary and document necessary exceptions.

Explain intent and consequences. Assertions include preconditions, postconditions and invariants. Standalone Classes permits small collaborations. Closure of Operations describes accepted result types without unrestricted algebra.

## Proportional verification

For code changes, run:

```powershell
docker compose up -d --wait
dotnet test RewardFlow.SuppleDesign.slnx --configuration Release
```

For core-only investigation, use `dotnet test RewardFlow.SuppleDesign.slnx --configuration Release --filter "FullyQualifiedName~DomainTests|FullyQualifiedName~ApplicationTests"`. This does not replace affected persistence/HTTP checks.

For docs, check links, examples, language, README alignment and implemented/proposed labels. Validate changed skills with skill-creator quick_validate.py when available. Do not test Markdown wording mechanically.

Current CI restores, builds Release and tests PostgreSQL. Checkout uses [actions/checkout@v7](https://github.com/actions/checkout/tree/v7) and SDK setup uses [actions/setup-dotnet@v6](https://github.com/actions/setup-dotnet/tree/v6), both declaring the Node.js 24 runtime. The workflow uses GitHub-hosted ubuntu-latest runners. It has no dedicated architecture, language, skill-selection or educational-quality check. Report unexecuted checks and causes.

## People and tools

Read [AGENTS.md](../AGENTS.md) and relevant skills. Compatible tools discover .agents/skills; others need explicit context. People use the same criteria. Skills do not authorize publishing, messaging or unrelated external actions.
