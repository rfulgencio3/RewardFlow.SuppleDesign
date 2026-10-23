<a id="top"></a>

<div align="center">

**English** · [Português (Brasil)](README.pt-BR.md)

# 🏆 RewardFlow.SuppleDesign

### Supple Design, Clean Architecture and Hexagonal Architecture in a practical .NET 8 API

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![Architecture](https://img.shields.io/badge/architecture-Clean_%2B_Hexagonal-0B7285?style=flat-square)](#architecture-clean-architecture-and-hexagonal-ports)
[![DDD](https://img.shields.io/badge/DDD-Supple_Design-E85D04?style=flat-square)](#how-the-code-applies-evans-supple-design-patterns)
[![CI](https://img.shields.io/github/actions/workflow/status/rfulgencio3/RewardFlow.SuppleDesign/ci.yml?branch=main&style=flat-square&label=CI)](https://github.com/rfulgencio3/RewardFlow.SuppleDesign/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-xUnit-5C2D91?style=flat-square&logo=dotnet&logoColor=white)](#tests)

A runnable loyalty and reward-points API demonstrating the Supple Design patterns from Chapter 10 of Eric Evans' *Domain-Driven Design*.

[Getting started](#run-and-verify) · [Wallet actions](#how-to-perform-wallet-actions) · [Request flow](#how-a-request-flows-through-the-project) · [Architecture](#architecture-clean-architecture-and-hexagonal-ports) · [Supple Design](#how-the-code-applies-evans-supple-design-patterns) · [Contributing](#contributing)

</div>

---

The goal is to make business code easy to read, reason about and change. Small, explicit domain concepts do the business work; application use cases coordinate them; adapters handle HTTP and storage.

> [!NOTE]
> This is an executable teaching project with real validation, concurrency protection and integration tests. It is intentionally smaller than a production loyalty platform.

| At a glance | Choice |
| --- | --- |
| Runtime | .NET 8 and ASP.NET Core Web API |
| Domain | Loyalty wallet and reward points |
| Architecture | Clean Architecture with Hexagonal Ports & Adapters |
| Persistence | PostgreSQL 17 through EF Core and Npgsql, with optimistic concurrency |
| API discovery | Swagger UI and OpenAPI |
| Verification | Domain, application and HTTP integration tests |

## Table of contents

- [What is RewardFlow?](#what-is-rewardflow)
- [Supple Design assessment](#supple-design-assessment)
- [Run and verify](#run-and-verify)
- [How to perform wallet actions](#how-to-perform-wallet-actions)
- [Ubiquitous Language and rules](#ubiquitous-language-and-rules)
- [How a request flows through the project](#how-a-request-flows-through-the-project)
- [Architecture: Clean Architecture and Hexagonal Ports](#architecture-clean-architecture-and-hexagonal-ports)
- [Project structure](#project-structure)
- [Evans' model building blocks and RewardFlow](#evans-model-building-blocks-and-rewardflow)
- [How the code applies Evans' Supple Design patterns](#how-the-code-applies-evans-supple-design-patterns)
- [Abstraction budget](#abstraction-budget)
- [HTTP contract and errors](#http-contract-and-errors)
- [Persistence and concurrency](#persistence-and-concurrency)
- [Database schema (ERD)](#database-schema-erd)
- [Migrations](#migrations)
- [Tests](#tests)
- [Future educational improvements](#future-educational-improvements)
- [Source and further reading](#source-and-further-reading)
- [Educational commentary disclaimer](#educational-commentary-disclaimer)
- [Contributing](#contributing)

## What is RewardFlow?

**RewardFlow** is a small loyalty and reward-points management system. Each member is represented by a rewards wallet that can be opened, credited when points are earned, debited when points are redeemed, queried for its current balance, and checked to determine whether it can afford a redemption.

The project focuses on this compact business flow so the domain language and design decisions remain easy to see. It is an educational model rather than a complete loyalty platform: member enrollment, campaigns, points expiration, authentication and a transaction ledger are outside its current scope.

## Supple Design assessment

> [!IMPORTANT]
> **RewardFlow fulfills the purpose of Supple Design within the scope of an educational sample.** Its model speaks the language of rewards, protects invariants where the state lives, keeps queries and calculations predictable, and isolates business decisions from HTTP and persistence.

The flexibility comes from explicit concepts and replaceable boundaries, not from speculative layers. PostgreSQL is implemented as a secondary adapter while EF Core remains outside the Domain and Application projects. The sample still omits factories, domain services, messaging and other mechanisms without a concrete modeling need. The [pattern mapping](#how-the-code-applies-evans-supple-design-patterns) provides the code-level evidence; the [abstraction budget](#abstraction-budget) explains why each boundary exists and when it should be reconsidered.

---

## Run and verify

### Prerequisites

Install Docker Desktop, a .NET SDK with `.slnx` support (9.0.200 or later), and the ASP.NET Core 8 runtime. The first restore needs access to NuGet for EF Core, Npgsql, test packages, Swagger tooling and, when necessary, .NET 8 targeting packs. The Domain and Application projects remain free of third-party package dependencies.

### Start the API

From the repository root, restore, build, test and run:

```powershell
docker compose up -d --wait
dotnet restore RewardFlow.SuppleDesign.slnx
dotnet build RewardFlow.SuppleDesign.slnx --no-restore
dotnet test RewardFlow.SuppleDesign.slnx --no-build
dotnet run --project src/RewardFlow.SuppleDesign.Api
```

> [!TIP]
> Open [http://localhost:5180/](http://localhost:5180/) after the API starts. The root redirects to Swagger UI, where every endpoint can be explored and executed. The raw OpenAPI document is available at `/swagger/v1/swagger.json`.

Visual Studio 2022 17.11 or later also discovers the shared `RewardFlow API` startup profile from [`RewardFlow.SuppleDesign.slnLaunch`](RewardFlow.SuppleDesign.slnLaunch) when the solution is opened.

Compose starts the pinned PostgreSQL image with the local development credentials from `compose.yaml`. The API applies pending EF Core migrations before accepting requests, so the first start creates the schema automatically. Wallets remain available across API restarts because PostgreSQL stores them in the named Docker volume.

## How to perform wallet actions

Start the API first, then choose one of these entry points: execute requests in Swagger at [http://localhost:5180/](http://localhost:5180/), open [`RewardFlow.http`](src/RewardFlow.SuppleDesign.Api/RewardFlow.http) in a compatible HTTP client, or use the PowerShell walkthrough below.

Use one wallet ID throughout a flow. A wallet must be opened before any command or query addresses it.

| Goal | Request | Input | Expected result |
| --- | --- | --- | --- |
| Open a wallet | `POST /api/wallets/{id}` | A new non-empty GUID in the route | `201 Created`, zero points and a `Location` header for the balance |
| Credit earned points | `POST /api/wallets/{id}/credit` | `{ "points": 100 }` | `200 OK` with the updated balance |
| Debit redeemed points | `POST /api/wallets/{id}/debit` | `{ "points": 40 }` | `200 OK`, provided the wallet can afford the amount |
| Read the balance | `GET /api/wallets/{id}/balance` | No body | `200 OK` with the current points |
| Check affordability | `GET /api/wallets/{id}/can-afford?points=60` | Non-negative whole points in the query string | `200 OK` with `canAfford`; the balance is unchanged |

The commands below create a wallet, credit 100 points, debit 40, confirm that 60 points are affordable and then demonstrate an overdraft rejection:

<details>
<summary><strong>PowerShell: open, credit, debit and inspect a wallet</strong></summary>

<br />

```powershell
$baseUri = 'http://localhost:5180'
$walletId = [guid]::NewGuid()

# Open the wallet: 201 Created, zero points, Location header for its balance.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId"

# Earn 100 points: 200 OK.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/credit" `
    -ContentType 'application/json' -Body '{"points":100}'

# Redeem 40 points: 200 OK, 60 remain.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/debit" `
    -ContentType 'application/json' -Body '{"points":40}'

Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId/balance"

# Pure query: true, and the balance remains 60.
Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId/can-afford?points=60"

# Rejected: 422 Unprocessable Entity; balance remains 60.
# PowerShell reports an HTTP error for this intentional failure.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/debit" `
    -ContentType 'application/json' -Body '{"points":61}'
```

</details>

## Ubiquitous Language and rules

| Term | Meaning |
| --- | --- |
| Reward Points | Whole, non-negative units earned and redeemed by a member; represented by `RewardPoints`. |
| Rewards Wallet | Aggregate holding an identified member's available reward points. Member enrollment is outside this example. |
| Open | Create an identified wallet with zero points. |
| Credit | Add earned points to an existing wallet. |
| Debit | Remove redeemed points from an existing wallet. |
| Balance | Reward points currently available to spend. |
| Can Afford | Determine whether the current balance covers required points without changing state. |

Wallet IDs must be non-empty GUIDs. All wallets are opened explicitly; queries and commands never silently create them. A credit or debit must contain at least one point. A balance may be zero, and debiting exactly the balance is valid. An empty wallet cannot fund a positive redemption. `CanAfford(RewardPoints.Zero)` is true, while a zero debit is rejected because a command must move points.

`RewardPoints` uses `long` and accepts values from zero through `long.MaxValue`. Negative construction, subtraction below zero, and addition above the maximum are rejected with domain exceptions. Failed operations leave the original values and wallet balance intact. Record value equality lets two independently constructed equal point amounts compare equal.

## How a request flows through the project

The credit operation shows how the adapters and core collaborate without moving business rules into the controller or repository:

```mermaid
sequenceDiagram
    actor Client
    participant API as WalletsController
    participant UseCase as ICreditRewardPoints
    participant Repository as IRewardsWalletRepository
    participant Wallet as RewardsWallet
    participant Points as RewardPoints

    Client->>API: POST /api/wallets/{id}/credit
    API->>UseCase: ExecuteAsync(id, points)
    UseCase->>Repository: FindAsync(id)
    Repository-->>UseCase: detached WalletSnapshot
    UseCase->>Wallet: Credit(earnedPoints)
    Wallet->>Points: Add(earnedPoints)
    Points-->>Wallet: new RewardPoints
    UseCase->>Repository: TrySaveAsync(wallet, revision)
    Repository-->>UseCase: saved
    UseCase-->>API: WalletBalance
    API-->>Client: 200 WalletBalanceResponse
```

1. `WalletsController` validates and translates the HTTP request, then calls the inbound application port.
2. `CreditRewardPoints` coordinates the use case and asks the outbound repository port for a detached snapshot.
3. `RewardsWallet.Credit` expresses the state change in domain language and delegates immutable arithmetic to `RewardPoints.Add`.
4. The application saves with the loaded revision, so a concurrent update cannot be silently overwritten.
5. The use case returns an application result, which the controller converts into an API response DTO.

Debit follows the same command path. Balance and affordability are queries: they load a snapshot but never call `TrySaveAsync`; `CanAfford` does not mutate the aggregate. Known domain and application failures cross back to `ApiExceptionHandler`, which translates them into HTTP Problem Details at the outer boundary.

## Architecture: Clean Architecture and Hexagonal Ports

```mermaid
flowchart LR
    Client([HTTP Client<br/>Swagger / Integration Tests])

    subgraph Api["API · Primary Adapter"]
        direction TB
        Controller["WalletsController<br/>Routes · Validation · Status Codes"]
        DTOs["Request / Response DTOs<br/>Problem Details"]
        Composition["Program.cs<br/>Composition Root"]
        Controller --- DTOs
    end

    subgraph Application["Application · Use Cases"]
        direction TB
        InboundPorts["Inbound Ports<br/>IOpenRewardsWallet<br/>ICreditRewardPoints<br/>IDebitRewardPoints<br/>IGetWalletBalance<br/>ICheckWalletAffordability"]
        UseCases["Use Case Implementations<br/>Open · Credit · Debit · Balance · Affordability"]
        InboundPorts --> UseCases
    end

    subgraph Domain["Domain · Business Core"]
        direction TB
        WalletPort["IRewardsWallet<br/>Credit · Debit · CanAfford"]
        Wallet["RewardsWallet<br/>Aggregate"]
        Points["RewardPoints<br/>Immutable Value Object"]
        Rules["DomainAssertions<br/>DomainException"]
        RepositoryPort["IRewardsWalletRepository<br/>Outbound Port"]

        WalletPort --> Wallet
        Wallet -->|delegates arithmetic| Points
        Wallet -->|enforces invariants| Rules
        Points -->|validates values| Rules
    end

    subgraph Infrastructure["Infrastructure · Secondary Adapter"]
        Repository["PostgresRewardsWalletRepository<br/>Detached Snapshots<br/>Optimistic Concurrency"]
        DbContext["RewardFlowDbContext<br/>EF Core Mapping · Migrations"]
        Repository --> DbContext
    end

    Database[(PostgreSQL 17<br/>Docker Volume)]

    Client -->|HTTP request| Controller
    Controller -->|calls inbound port| InboundPorts
    UseCases -->|invokes domain behavior| WalletPort
    UseCases -->|loads / saves| RepositoryPort
    Repository -.->|implements| RepositoryPort
    DbContext -->|Npgsql| Database
    Composition -.->|binds ports to adapters| InboundPorts
    Composition -.-> Repository

    classDef external fill:#f5f5f5,stroke:#616161,color:#212121
    classDef adapter fill:#e3f2fd,stroke:#1976d2,color:#0d47a1
    classDef application fill:#fff3e0,stroke:#ef6c00,color:#e65100
    classDef domain fill:#e8f5e9,stroke:#388e3c,color:#1b5e20
    classDef port fill:#f3e5f5,stroke:#7b1fa2,color:#4a148c
    class Client,Database external
    class Controller,DTOs,Composition,Repository,DbContext adapter
    class UseCases application
    class Wallet,Points,Rules domain
    class InboundPorts,WalletPort,RepositoryPort port
```

The solid arrows show request flow and domain collaboration. The dashed arrows show adapter wiring: `Program.cs` selects implementations, while Infrastructure implements the outbound repository port owned by Domain.

The repository interface is owned by Domain as the requested outbound port. Application owns the inbound use-case interfaces. `IRewardsWallet` is the business behavior contract implemented by the aggregate, rather than a transport or persistence abstraction. The controllers receive the application interfaces through constructor injection.

Domain has no project or package references. Application and Infrastructure reference Domain independently. Infrastructure does not know about HTTP, and Application does not reference Infrastructure. API references Infrastructure only to select the concrete adapter in the composition root. Domain objects are never serialized directly as HTTP contracts.

## Project structure

The folders are architectural boundaries rather than technical buckets. Each project has one direction of responsibility and a deliberately small public surface:

```text
RewardFlow.SuppleDesign/
├── RewardFlow.SuppleDesign.slnx
├── RewardFlow.SuppleDesign.slnLaunch              # Shared Visual Studio startup profile
├── Directory.Build.props                         # Shared .NET 8 build settings
├── compose.yaml                                  # Local PostgreSQL 17 container and volume
├── dotnet-tools.json                             # Reproducible EF Core CLI version
├── README.md
│
├── src/                                           # Production code
│   ├── RewardFlow.SuppleDesign.Domain/            # Business model; no external dependencies
│   │   ├── Entities/
│   │   │   └── RewardsWallet.cs                   # Aggregate root
│   │   ├── ValueObjects/
│   │   │   └── RewardPoints.cs                    # Immutable value object
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   ├── Ports/
│   │   │   ├── IRewardsWallet.cs                  # Domain behavior contract
│   │   │   └── IRewardsWalletRepository.cs        # Outbound persistence port
│   │   ├── DomainAssertions.cs                    # Preconditions and invariants
│   │   └── RewardFlow.SuppleDesign.Domain.csproj
│   │
│   ├── RewardFlow.SuppleDesign.Application/       # Use-case orchestration
│   │   ├── Wallets/
│   │   │   ├── Common/                            # Results, exceptions and persistence coordination
│   │   │   ├── OpenWallet/
│   │   │   ├── CreditPoints/
│   │   │   ├── DebitPoints/
│   │   │   ├── GetBalance/
│   │   │   └── CheckAffordability/
│   │   └── RewardFlow.SuppleDesign.Application.csproj
│   │
│   ├── RewardFlow.SuppleDesign.Infrastructure/    # Secondary adapters
│   │   ├── Persistence/
│   │   │   ├── Configurations/                    # Fluent EF Core mapping
│   │   │   ├── Migrations/                        # Versioned PostgreSQL schema
│   │   │   ├── Models/                            # Persistence-only records
│   │   │   ├── RewardFlowDbContext.cs
│   │   │   └── RewardFlowDbContextFactory.cs      # Migration tooling
│   │   ├── Repositories/
│   │   │   └── PostgresRewardsWalletRepository.cs
│   │   ├── DependencyInjection.cs                 # Adapter registration and migration startup
│   │   └── RewardFlow.SuppleDesign.Infrastructure.csproj
│   │
│   └── RewardFlow.SuppleDesign.Api/               # HTTP primary adapter
│       ├── Endpoints/
│       │   └── Wallets/
│       │       └── WalletsController.cs
│       ├── DTOs/
│       │   └── Wallets/
│       │       └── WalletContracts.cs
│       ├── Errors/
│       │   └── ApiExceptionHandler.cs
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── Program.cs                             # Composition root
│       ├── appsettings.json                       # Connection string and runtime configuration
│       ├── RewardFlow.http                        # Executable HTTP examples
│       └── RewardFlow.SuppleDesign.Api.csproj
│
└── test/                                          # Executable specifications
    └── RewardFlow.SuppleDesign.Tests/
        ├── TestDoubles/
        │   └── InMemoryRewardsWalletRepository.cs
        ├── DomainTests.cs
        ├── ApplicationTests.cs
        ├── PostgresRepositoryTests.cs
        ├── WalletApiTests.cs
        └── RewardFlow.SuppleDesign.Tests.csproj
```

| Project | Owns | Depends on |
| --- | --- | --- |
| `RewardFlow.SuppleDesign.Domain` | Ubiquitous Language, invariants, aggregate behavior, value objects and outbound ports | Nothing |
| `RewardFlow.SuppleDesign.Application` | Use-case ports and workflow coordination | Domain |
| `RewardFlow.SuppleDesign.Infrastructure` | Replaceable implementations of outbound ports | Domain |
| `RewardFlow.SuppleDesign.Api` | HTTP endpoints, API contracts, error translation and dependency composition | Application and Infrastructure |
| `RewardFlow.SuppleDesign.Tests` | Executable examples across domain, application and HTTP boundaries | The system under test |

## Evans' model building blocks and RewardFlow

The supplied book figure presents Evans' broader **Model-Driven Design building blocks**. It complements Chapter 10, but it is not itself the Supple Design pattern diagram: Entities, Value Objects, Aggregates, Repositories, Services and Factories are tactical building blocks; Intention-Revealing Interfaces, Side-Effect-Free Functions, Assertions, Conceptual Contours, Standalone Classes and Closure of Operations make that model supple.

The correspondence in this solution is:

```mermaid
flowchart LR
    subgraph Evans["Evans · Model-Driven Design"]
        direction TB
        E["Entities"]
        V["Value Objects"]
        A["Aggregates"]
        R["Repositories"]
        S["Services"]
        F["Factories"]
        L["Layered Architecture"]
        U["Smart UI"]
    end

    subgraph RewardFlow["RewardFlow · Concrete Implementation"]
        direction TB
        Wallet["RewardsWallet<br/>Entity + Aggregate Root"]
        Points["RewardPoints<br/>Immutable Value Object"]
        Repository["IRewardsWalletRepository<br/>+ PostgreSQL Adapter"]
        UseCases["Application Use Cases<br/>Open · Credit · Debit · Balance · Affordability"]
        Construction["Constructors<br/>Simple creation; no Factory needed"]
        Layers["Domain · Application<br/>Infrastructure · API"]
        Controllers["Thin Controllers<br/>HTTP concerns only"]
    end

    E --> Wallet
    A --> Wallet
    V --> Points
    R --> Repository
    S -.->|application coordination| UseCases
    F -.->|creation is still simple| Construction
    L --> Layers
    U -.->|alternative deliberately avoided| Controllers

    classDef evans fill:#f5f5f5,stroke:#616161,color:#212121
    classDef domain fill:#e8f5e9,stroke:#388e3c,color:#1b5e20
    classDef application fill:#fff3e0,stroke:#ef6c00,color:#e65100
    classDef adapter fill:#e3f2fd,stroke:#1976d2,color:#0d47a1
    class E,V,A,R,S,F,L,U evans
    class Wallet,Points domain
    class UseCases application
    class Repository,Layers,Controllers,Construction adapter
```

| Evans concept | RewardFlow parallel | Interpretation |
| --- | --- | --- |
| Entity | `RewardsWallet` | Has a stable `Guid` identity and behavior that changes its balance. |
| Value Object | `RewardPoints` | Immutable, compared by value, self-validating and closed under `Add` and valid `Subtract` operations. |
| Aggregate | `RewardsWallet` | Serves as the consistency boundary and root for credits, debits and affordability rules. |
| Repository | `IRewardsWalletRepository` | The Domain owns the outbound port; `PostgresRewardsWalletRepository` is an Infrastructure adapter, while tests use an in-memory double. |
| Service | Application use cases | The nearest parallel coordinates workflows. They are application services, not domain services: the business rules remain in `RewardsWallet` and `RewardPoints`. |
| Factory | Constructors | Creation is simple enough to remain in constructors. A dedicated Factory would add ceremony without protecting a new invariant. |
| Layered Architecture | Four projects | Domain is isolated; Application coordinates it; Infrastructure and API are adapters around it. |
| Smart UI | Deliberately absent | Controllers contain HTTP translation only. They do not own reward rules, which prevents the UI from becoming the domain model. |

The Mermaid diagram is a project-specific interpretation of the model-building blocks described by Evans; it is not a reproduction of a figure from the book. The bibliographic links in [Source and further reading](#source-and-further-reading) point readers to the original material.

## How the code applies Evans' Supple Design patterns

Chapter 10 moves from identifying domain building blocks to shaping code that communicates its purpose. The examples below show where each pattern appears and what design pressure it resolves in RewardFlow.

| Pattern | Code | Why it matters |
| --- | --- | --- |
| Intention-Revealing Interfaces | `IRewardsWallet.Credit`, `Debit`, `CanAfford`; named application ports; repository operations | Readers use the domain's vocabulary instead of generic setters or opaque processing methods. |
| Side-Effect-Free Functions | `RewardsWallet.CanAfford`; `RewardPoints.Add` and `Subtract` | Queries and calculations do not mutate their inputs. Affordability observes the wallet's current balance; arithmetic returns new immutable values. State changes are explicit `Credit` and `Debit` commands. |
| Assertions | `DomainAssertions`, `RewardPoints` construction/overflow protection, aggregate commands | Preconditions and post-state invariants are enforced at runtime rather than left to caller convention or debug-only assertions. HTTP validation supplements these rules; direct domain callers still receive full protection. |
| Conceptual Contours | `RewardPoints`, `RewardsWallet`, use cases, controllers, repository port and adapter | Arithmetic, aggregate state, orchestration, transport, and persistence each have a distinct reason to change. Persistence revision metadata stays outside the aggregate. |
| Standalone Classes | `RewardPoints` | The value object carries its own immutable value, equality, arithmetic, and validity rules. It requires no entity, repository, DI container, ORM, or web framework. Its only collaborators are BCL types and small local domain assertion/exception helpers. |
| Closure of Operations | `RewardPoints.Add(RewardPoints)` and `Subtract(RewardPoints)` | Both operands and successful results remain in the same conceptual set, so expressions compose naturally without unpacking primitives. Operations are partial over non-negative bounded integers: insufficient subtraction or overflow raises an explicit domain exception. |

For example, `new RewardPoints(100).Add(new RewardPoints(25)).Subtract(new RewardPoints(40))` yields 85 points and leaves every operand unchanged. `RewardsWallet.Credit` delegates to that arithmetic and replaces its balance only after validation succeeds. `CanAfford` performs no writes, calls no external service, and has no clock or randomness dependency.

XML comments above public classes, interfaces, and methods identify the relevant patterns. Implementations use `<inheritdoc />` where the interface already describes the behavior. These patterns reinforce one another; the comments explain design intent rather than claim every I/O operation is a pure function.

## Abstraction budget

Supple Design favors code that exposes the model with the least machinery needed to protect it. Every abstraction in this sample therefore needs a current reason to exist and a clear condition under which it should be reconsidered.

| Abstraction | Why it exists now | Reconsider it when |
| --- | --- | --- |
| `RewardPoints` | Gives point arithmetic, range checks and value equality one domain home instead of scattering primitive `long` operations. | Points no longer have behavior or invariants beyond the primitive value. |
| `RewardsWallet` | Defines the consistency boundary for identity, balance and credit/debit behavior. | Wallet behavior separates into independently consistent concepts. |
| `IRewardsWallet` | Names the small domain role required to demonstrate an intention-revealing interface. It is not presented as a persistence or DI seam, and `RewardsWallet` is its only implementation. | The teaching goal no longer needs the explicit role contract, or a real second domain implementation changes its shape. |
| `DomainAssertions` | Shares domain assertions used across `RewardPoints` and `RewardsWallet` while preserving domain-specific error codes. | A rule belongs to only one type; move it beside that state instead of growing a generic validation utility. |
| Application use-case interfaces | Act as inbound ports so the HTTP controller depends on capabilities such as crediting and checking affordability, not concrete orchestration. | The application is no longer consumed through an adapter boundary, or several ports acquire identical semantics and can be combined without losing language. |
| `WalletBalance` and `WalletAffordability` | Return application results without coupling use cases to HTTP DTOs or exposing mutable aggregates. | A result has exactly one consumer and no boundary-specific representation to isolate. |
| `IRewardsWalletRepository` | Is the outbound persistence port; PostgreSQL and the test double satisfy the same application contract. | Storage is no longer replaceable or persistence behavior becomes part of a different consistency boundary. |
| `WalletSnapshot` and revision checks | Prevent concurrent commands from silently overwriting a balance loaded from an older revision. | The chosen store supplies an equivalent concurrency contract through a simpler port. |
| `WalletPersistence` | Centralizes the repeated not-found and optimistic-save coordination shared by several use cases. | Only one caller remains or the shared workflow begins hiding different business decisions. |
| `RewardFlowDbContext` and persistence models | Translate between relational storage and detached aggregates without adding EF Core annotations or tracking behavior to the Domain. | A different adapter replaces EF Core, or a mapping becomes simple enough to live entirely inside its repository. |
| API DTOs and `ApiExceptionHandler` | Keep validation, serialization, status codes and Problem Details at the HTTP boundary. | The primary adapter changes; replace these contracts rather than leaking HTTP into the core. |

The project deliberately has no generic repository, Unit of Work wrapper, mediator, mapping framework, factory, domain service or event bus. None currently protects an additional invariant or supports a second adapter. A new layer or interface should be introduced only when a concrete requirement creates that boundary; a pattern name alone is not sufficient justification.

## HTTP contract and errors

| Method | Route | Request body | Success |
| --- | --- | --- | --- |
| POST | `/api/wallets/{id}` | none | 201 and balance location |
| POST | `/api/wallets/{id}/credit` | `{"points":100}` | 200 and updated balance |
| POST | `/api/wallets/{id}/debit` | `{"points":40}` | 200 and updated balance |
| GET | `/api/wallets/{id}/balance` | none | 200 and current balance |
| GET | `/api/wallets/{id}/can-afford?points=60` | none | 200 and affordability result |

The response body is `{"walletId":"<guid>","points":60}`. IDs in the route must parse as GUIDs; malformed GUID routes do not match and return 404. An all-zero GUID parses but violates a domain invariant and returns 422.

| Status | Situation |
| --- | --- |
| 400 | Invalid JSON, omitted/null points, non-positive points, fractional points, or values outside the `long` range. `[ApiController]` performs contract validation. |
| 404 | A well-formed wallet ID is not found. |
| 409 | Duplicate opening or a stale concurrent write. |
| 422 | Domain rule violation, such as insufficient points, balance overflow, or an empty GUID. |
| 500 | Unexpected failure; the public detail does not expose internal exception information. |

Expected application/domain failures use `application/problem+json` with an additional stable `code`, for example:

```json
{
  "title": "Reward rule violated",
  "status": 422,
  "detail": "The rewards wallet cannot afford the required points.",
  "code": "insufficient_points"
}
```

ASP.NET Core may add standard fields such as `type` or a trace ID. Automatic request validation uses the framework's validation Problem Details with an `errors` collection. Exception-to-HTTP mapping lives exclusively in the API project.

## Persistence and concurrency

`PostgresRewardsWalletRepository` implements the domain-owned repository port through EF Core and Npgsql. The adapter maps a persistence-only `RewardsWalletState` to a fresh `RewardsWallet` and immutable `RewardPoints` on every load. The aggregate has no EF Core attributes, proxy requirements, navigation properties or persistence constructor.

Opening inserts revision `0`; PostgreSQL's primary key makes duplicate opening atomic. Saving issues one conditional update matching both `id` and the expected `revision`, then increments the revision. If another command has already saved that snapshot, zero rows are updated and the application returns 409. A failed domain operation never reaches persistence.

When two updates race, one can return 409. The client should reload the balance and reconsider/retry the operation. The demo does not automatically retry commands or implement idempotency keys. Repeating a successful credit/debit applies it again. A production adapter could implement the same revision contract with a database concurrency token and a transaction.

The connection string is read from `ConnectionStrings:RewardFlow`. Local settings match `compose.yaml`; deployments can override it with the `ConnectionStrings__RewardFlow` environment variable. The checked-in password is only a local development credential.

### Database schema (ERD)

The domain currently needs one persistence table. `RewardPoints` is flattened into the `points` column because it is a value object owned by the wallet, while `revision` belongs to persistence coordination rather than to the aggregate.

```mermaid
erDiagram
    REWARDS_WALLETS {
        uuid id PK "aggregate identity"
        bigint points "non-negative balance"
        bigint revision "optimistic concurrency"
    }
```

Both numeric columns have non-negative database check constraints. EF Core also creates `__EFMigrationsHistory`; it is tooling metadata and is intentionally omitted from the domain-oriented ERD.

### Migrations

The initial migration is versioned under `Infrastructure/Persistence/Migrations`. `Program.cs` calls `ApplyRewardFlowMigrationsAsync` before middleware is activated, and that adapter helper invokes `Database.MigrateAsync`. Applying the same migration again is safe because EF Core records completed migrations.

To create a migration after changing only the persistence mapping:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add DescribeTheSchemaChange `
    --project src/RewardFlow.SuppleDesign.Infrastructure/RewardFlow.SuppleDesign.Infrastructure.csproj `
    --output-dir Persistence/Migrations
```

> [!CAUTION]
> Runtime migration is intentional for this self-contained educational project. For production, inspect migrations and prefer a reviewed SQL script or migration bundle executed by the deployment process with dedicated schema permissions. The application identity should normally have only data read/write permissions.

Useful local database commands:

```powershell
# Stop containers and retain wallet data.
docker compose down

# Delete the local database volume and start from an empty database.
docker compose down --volumes
```

The sample does not include authentication, authorization, a ledger, member enrollment, reward expiration or transaction-wide integration events. Add those around the explicit business model and ports as requirements evolve. The local HTTP and database credentials are for running the demo; deployment security is outside this example's scope.

## Tests

The xUnit suite covers:

- Value equality, new-value arithmetic, operand immutability, bounds, overdrafts, and overflow.
- Exact-balance redemption, zero-balance protection, empty identity, and rejected mutations preserving state.
- Repeated affordability queries preserving the same balance reference.
- Open/credit/debit/read orchestration, missing-wallet reads, duplicate IDs, cancellation, detached snapshots, and stale-write rejection.
- PostgreSQL mapping, startup migration, durable API persistence and conditional revision updates through the real adapter.
- Real ASP.NET Core routing and DI through `WebApplicationFactory`, response contracts, 201/200/400/404/409/422 behavior, Problem Details, and malformed request bodies.

Every database test uses fresh wallet identities. Start PostgreSQL with `docker compose up -d --wait`; `dotnet test RewardFlow.SuppleDesign.slnx` then builds the solution, applies pending migrations and executes the suite without starting an external HTTP server. Application tests use the in-memory repository only as a fast test double for the same port semantics.

## Future educational improvements

Future additions should introduce a concrete modeling pressure and make its effect on the design visible. The purpose is to deepen the Supple Design example without turning the repository into a catalog of infrastructure patterns.

| Learning increment | Domain question to explore | Supple Design focus |
| --- | --- | --- |
| Reward transaction history | Is a credit or debit merely a balance change, or does each movement have identity, reason and audit meaning? | Discover new Conceptual Contours and decide whether a transaction is an Entity inside the wallet boundary or belongs to another Aggregate. |
| Points expiration | How should points earned at different times expire while preserving valid redemption rules? | Introduce explicit temporal concepts, Intention-Revealing Interfaces and Assertions instead of spreading date comparisons across use cases. |
| Configurable earning policy | How are purchase amount, campaign and membership tier converted into points? | Model the calculation as a Standalone Class with a Side-Effect-Free Function whose inputs and result are explicit. |
| Transfer between wallets | Which rules and consistency boundary govern movement involving two wallets? | Evaluate whether the behavior belongs to an Aggregate or requires a Domain Service, based on the model rather than the pattern name. |
| Idempotent commands | How can a retried credit avoid applying the same business event twice? | Give command identity and duplicate handling clear names while keeping transport and persistence details outside the reward model. |
| Algebraic and property-based examples | Which properties always hold for reward-point operations? | Demonstrate Closure of Operations, immutability and invariants with properties such as additive identity and rejected underflow. |

Each increment should begin with examples, vocabulary and failing tests. Only then should the model change. The README and abstraction budget should be updated with the new decision, alternatives considered and the condition that justifies every new type or boundary.

## Source and further reading

This project is inspired by Eric Evans' *Domain-Driven Design: Tackling Complexity in the Heart of Software* (Addison-Wesley, 2003, ISBN `978-0-321-12521-7`).

> [!IMPORTANT]
> The main reference is **Part III, Chapter 10 — “Supple Design.”** It connects deep modeling with code that is clear to use, predictable to change and suitable for continuous refactoring.

| Resource | Use it for |
| --- | --- |
| [Official book page — InformIT](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217) | Description, publication details and purchase options |
| [Chapter 10: Supple Design — O'Reilly Learning](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) | The chapter catalog and reading page; full access may require a subscription or purchased copy |
| [DDD Reference — Domain Language](https://www.domainlanguage.com/ddd/reference/) | Eric Evans' free companion summary of the book's definitions and patterns |
| [Npgsql EF Core provider](https://www.npgsql.org/efcore/) | PostgreSQL provider configuration and EF Core integration |
| [Applying EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) | Migration deployment strategies and runtime-migration trade-offs |

## Educational commentary disclaimer

> [!NOTE]
> This repository is intentionally more heavily commented than typical production code. The XML documentation names the pattern being demonstrated, explains the design decision and helps readers connect implementation details with the underlying DDD concepts. In a production codebase, comments should usually explain non-obvious intent and trade-offs rather than restate the code.

The central domain types include XML `<seealso>` references that IDEs can present as navigable documentation links:

| Code | Teaching focus | Reference |
| --- | --- | --- |
| [`RewardPoints`](src/RewardFlow.SuppleDesign.Domain/ValueObjects/RewardPoints.cs) | Standalone Class, Closure of Operations and Side-Effect-Free Functions | [Chapter 10: Supple Design](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [`IRewardsWallet`](src/RewardFlow.SuppleDesign.Domain/Ports/IRewardsWallet.cs) | Intention-Revealing Interfaces | [Chapter 10: Supple Design](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [`DomainAssertions`](src/RewardFlow.SuppleDesign.Domain/DomainAssertions.cs) | Assertions and explicit invariants | [Chapter 10: Supple Design](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [`RewardsWallet`](src/RewardFlow.SuppleDesign.Domain/Entities/RewardsWallet.cs) | Conceptual Contours, Entity and Aggregate Root | [DDD Reference](https://www.domainlanguage.com/ddd/reference/) |
| [`IRewardsWalletRepository`](src/RewardFlow.SuppleDesign.Domain/Ports/IRewardsWalletRepository.cs) | Repository as an outbound domain port | [DDD Reference](https://www.domainlanguage.com/ddd/reference/) |

These links are study aids, not claims that this sample is the only valid implementation of Evans' ideas. The design choices are adaptations for a small C# API and should be reconsidered as domain complexity grows.

## Contributing

RewardFlow.SuppleDesign is a collaborative project. Improvements, constructive criticism and suggestions are always welcome.

To contribute code or documentation:

1. Fork the repository.
2. Create a branch describing the change.
3. Implement the improvement and keep the architectural boundaries explicit.
4. Run `dotnet test RewardFlow.SuppleDesign.slnx`.
5. Open a pull request explaining the motivation, the resulting behavior and how the change was validated.

Contributions can range from typo fixes and clearer examples to new adapters, stronger tests and alternative demonstrations of Supple Design. If you find something confusing or disagree with a design decision, feel free to open a discussion or pull request—the feedback is part of improving the project.

---

<div align="center">

Built as a collaborative study of domain modeling and supple software design.

[Back to top](#top)

</div>
