<a id="inicio"></a>

<div align="center">

[English](README.md) · **Português (Brasil)**

# 🏆 RewardFlow.SuppleDesign

### Supple Design, Clean Architecture e Arquitetura Hexagonal em uma API prática com .NET 8

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![Arquitetura](https://img.shields.io/badge/arquitetura-Clean_%2B_Hexagonal-0B7285?style=flat-square)](#arquitetura-clean-architecture-e-portas-hexagonais)
[![DDD](https://img.shields.io/badge/DDD-Supple_Design-E85D04?style=flat-square)](#como-o-código-aplica-os-padrões-de-supple-design-de-evans)
[![CI](https://img.shields.io/github/actions/workflow/status/rfulgencio3/RewardFlow.SuppleDesign/ci.yml?branch=main&style=flat-square&label=CI)](https://github.com/rfulgencio3/RewardFlow.SuppleDesign/actions/workflows/ci.yml)
[![Testes](https://img.shields.io/badge/testes-xUnit-5C2D91?style=flat-square&logo=dotnet&logoColor=white)](#testes)

Uma API executável de fidelidade e pontos de recompensa que demonstra os padrões de **Supple Design** apresentados no Capítulo 10 de *Domain-Driven Design*, de Eric Evans.

[Começar](#executar-e-verificar) · [Ações da carteira](#como-realizar-ações-na-carteira) · [Fluxo de uma requisição](#como-uma-requisição-percorre-o-projeto) · [Arquitetura](#arquitetura-clean-architecture-e-portas-hexagonais) · [Supple Design](#como-o-código-aplica-os-padrões-de-supple-design-de-evans) · [Contribuir](#como-contribuir)

</div>

---

O objetivo é tornar o código de negócio fácil de ler, compreender e modificar. Conceitos pequenos e explícitos executam as regras do domínio; os casos de uso da aplicação coordenam esses conceitos; os adaptadores cuidam de HTTP e persistência.

| Visão geral | Escolha |
| --- | --- |
| Runtime | .NET 8 e ASP.NET Core Web API |
| Domínio | Carteira de fidelidade e pontos de recompensa |
| Arquitetura | Clean Architecture com Portas e Adaptadores Hexagonais |
| Persistência | PostgreSQL 17 com EF Core e Npgsql, incluindo concorrência otimista |
| Descoberta da API | Swagger UI e OpenAPI |
| Verificação | Testes de domínio, aplicação, persistência e HTTP |

## Índice

- [O que é o RewardFlow?](#o-que-é-o-rewardflow)
- [Avaliação de Supple Design](#avaliação-de-supple-design)
- [Executar e verificar](#executar-e-verificar)
- [Como realizar ações na carteira](#como-realizar-ações-na-carteira)
- [Linguagem Ubíqua e regras](#linguagem-ubíqua-e-regras)
- [Como uma requisição percorre o projeto](#como-uma-requisição-percorre-o-projeto)
- [Arquitetura: Clean Architecture e Portas Hexagonais](#arquitetura-clean-architecture-e-portas-hexagonais)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Blocos de construção de Evans e o RewardFlow](#blocos-de-construção-de-evans-e-o-rewardflow)
- [Como o código aplica os padrões de Supple Design de Evans](#como-o-código-aplica-os-padrões-de-supple-design-de-evans)
- [Orçamento de abstrações](#orçamento-de-abstrações)
- [Decisões arquiteturais](#decisões-arquiteturais)
- [Contrato HTTP e erros](#contrato-http-e-erros)
- [Persistência e concorrência](#persistência-e-concorrência)
- [Modelo de dados (DER)](#modelo-de-dados-der)
- [Migrations](#migrations)
- [Testes](#testes)
- [Melhorias educacionais futuras](#melhorias-educacionais-futuras)
- [Fontes e leituras complementares](#fontes-e-leituras-complementares)
- [Aviso sobre os comentários educacionais](#aviso-sobre-os-comentários-educacionais)
- [Como contribuir](#como-contribuir)
- [Documentação, skills e guardrails](#documentação-skills-e-guardrails)

## O que é o RewardFlow?

O **RewardFlow** é um pequeno sistema de gestão de fidelidade e pontos de recompensa. Cada membro é representado por uma carteira de recompensas, que pode ser aberta, receber créditos quando pontos são ganhos, receber débitos quando pontos são resgatados, informar o saldo atual e verificar se possui pontos suficientes para um resgate.

O projeto se concentra nesse fluxo de negócio compacto para manter visíveis a linguagem do domínio e as decisões de design. Cada crédito e débito possui identidade, motivo e registro imutável para auditoria. Ele é um modelo educacional, não uma plataforma completa de fidelidade: cadastro de membros, campanhas, expiração de pontos e autenticação estão fora do escopo atual.

## Avaliação de Supple Design

> [!IMPORTANT]
> **O RewardFlow atende ao propósito de Supple Design dentro do escopo de um exemplo educacional.** O modelo fala a linguagem de recompensas, protege as invariantes onde o estado reside, mantém consultas e cálculos previsíveis e isola as decisões de negócio de HTTP e persistência.

A flexibilidade vem de conceitos explícitos e fronteiras substituíveis. O PostgreSQL é um adaptador secundário, enquanto o EF Core permanece fora dos projetos Domain e Application. O exemplo não adiciona factories ou mensageria sem necessidade concreta. RewardTransferCalculator é um serviço de domínio justificado pela regra entre duas carteiras. O [mapeamento dos padrões](#como-o-código-aplica-os-padrões-de-supple-design-de-evans) mostra as evidências no código, e o [orçamento de abstrações](#orçamento-de-abstrações) explica por que cada fronteira existe.

---

## Executar e verificar

### Pré-requisitos

Instale o Docker Desktop e o runtime do ASP.NET Core 8. O `global.json` do repositório seleciona a faixa do SDK .NET 9 necessária para arquivos `.slnx`; instale um SDK .NET 9 compatível caso ainda não esteja disponível. O primeiro restore precisa de acesso ao NuGet para obter EF Core, Npgsql, pacotes de teste, Swagger e, quando necessário, os targeting packs do .NET 8. Domain e Application continuam sem dependências de pacotes de terceiros.

### Iniciar a API

Na raiz do repositório:

~~~powershell
docker compose up -d --wait
dotnet restore RewardFlow.SuppleDesign.slnx
dotnet build RewardFlow.SuppleDesign.slnx --no-restore
dotnet test RewardFlow.SuppleDesign.slnx --no-build
dotnet run --project src/RewardFlow.SuppleDesign.Api
~~~

> [!TIP]
> Abra [http://localhost:5180/](http://localhost:5180/) após iniciar a API. A raiz redireciona para o Swagger UI, onde todos os endpoints podem ser consultados e executados. O documento OpenAPI está disponível em /swagger/v1/swagger.json.

A inicialização normal suprime os comandos do EF Core e mostra somente etapas úteis, em inglês:

~~~text
RewardFlow API
[1/3] Preparing application services...
[2/3] Connecting to PostgreSQL and applying migrations...
[OK]  Database is ready.
[3/3] Starting the HTTP server...
[OK]  RewardFlow API is ready.
      Swagger: http://localhost:5180/swagger
      Press Ctrl+C to stop.
~~~

O Compose inicia uma imagem fixada do PostgreSQL com as credenciais locais de desenvolvimento definidas em compose.yaml. Antes de aceitar requisições, a API aplica as migrations pendentes do EF Core. Na primeira execução, o banco e a tabela são criados automaticamente. As carteiras permanecem disponíveis depois que a API é reiniciada porque o PostgreSQL usa um volume Docker nomeado.

## Como realizar ações na carteira

Todas as rotas abaixo estão implementadas. A transferência move 1..100 pontos por operação, exige saldo suficiente na origem e confirma ambas as carteiras atomicamente; consulte o [guia de implementação](docs/transfer-plan.md).

Inicie a API e escolha uma destas opções: execute as requisições pelo Swagger em [http://localhost:5180/](http://localhost:5180/), abra [RewardFlow.http](src/RewardFlow.SuppleDesign.Api/RewardFlow.http) em um cliente HTTP compatível ou siga o exemplo em PowerShell.

Mantenha cada ID de carteira durante seu fluxo; transferências exigem duas carteiras previamente abertas. Preserve operationId somente ao repetir a mesma operação.

| Objetivo | Requisição | Entrada | Resultado esperado |
| --- | --- | --- | --- |
| Abrir uma carteira | POST /api/wallets | { "name": "Recompensas principais" } | 201 Created, walletId gerado e header Location |
| Consultar a carteira | GET /api/wallets/{id} | Sem body | 200 OK com nome, status, criação e saldo |
| Creditar pontos ganhos | POST /api/wallets/{id}/credit | `{ "operationId": "uuid", "points": 100, "reason": "Compra" }` | 200 OK com a movimentação e o saldo resultante |
| Debitar pontos resgatados | POST /api/wallets/{id}/debit | `{ "operationId": "uuid", "points": 40, "reason": "Resgate" }` | 200 OK, se houver saldo suficiente |
| Transferir pontos | POST /api/wallets/{sourceWalletId}/transfers | operationId, destinationWalletId, 1..100 pontos e reason | 200 com ambos os saldos; origem com saldo suficiente; gravação atômica |
| Consultar o saldo | GET /api/wallets/{id}/balance | Sem body | 200 OK com os pontos atuais |
| Verificar capacidade de pagamento | GET /api/wallets/{id}/can-afford?requiredPoints=60 | Pontos inteiros não negativos | 200 OK com canAfford; o saldo não muda |
| Consultar o extrato | GET /api/wallets/{id}/transactions?page=1&pageSize=20 | Paginação entre 1 e 100 itens | 200 OK em ordem cronológica inversa com `hasNextPage` |
| Suspender / reativar / encerrar | POST /api/wallets/{id}/{ação} | Sem body | 204 No Content após uma transição válida |

<details>
<summary><strong>PowerShell: abrir, creditar, debitar, transferir e consultar carteiras</strong></summary>

<br />

~~~powershell
$baseUri = 'http://localhost:5180'
# Open the wallet: the API generates its identity and returns it with 201 Created.
$opened = Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets" `
    -ContentType 'application/json' -Body '{"name":"Primary rewards"}'
$walletId = $opened.walletId

Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId"

# Earn 100 points: 200 OK.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/credit" `
    -ContentType 'application/json' `
    -Body (@{ operationId = [guid]::NewGuid(); points = 100; reason = 'Purchase reward' } | ConvertTo-Json)

# Redeem 40 points: 200 OK, 60 remain.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/debit" `
    -ContentType 'application/json' `
    -Body (@{ operationId = [guid]::NewGuid(); points = 40; reason = 'Gift redemption' } | ConvertTo-Json)

Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId/balance"

# Read-only query: true, and the balance remains 60.
Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId/can-afford?requiredPoints=60"

# Rejected: 422 Unprocessable Entity; balance remains 60.
# PowerShell reports an HTTP error for this intentional failure.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/debit" `
    -ContentType 'application/json' `
    -Body (@{ operationId = [guid]::NewGuid(); points = 61; reason = 'Overdraft example' } | ConvertTo-Json)

# Open the destination, then transfer 40 points: source=20, destination=40.
$destination = Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets" `
    -ContentType 'application/json' -Body '{"name":"Transfer destination"}'
$transferOperationId = [guid]::NewGuid()
$transferBody = @{ operationId = $transferOperationId; destinationWalletId = $destination.walletId; points = 40; reason = 'Reward points transfer' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/transfers" `
    -ContentType 'application/json' -Body $transferBody

# An identical retry returns the original result without moving more points.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/transfers" `
    -ContentType 'application/json' -Body $transferBody

~~~

</details>

## Linguagem Ubíqua e regras

| Termo | Significado |
| --- | --- |
| Reward Points | Unidades inteiras e não negativas, ganhas e resgatadas por um membro; representadas por RewardPoints. |
| Rewards Wallet | Agregado que mantém os pontos disponíveis de um membro identificado. |
| Open | Criar uma carteira identificada com zero pontos. |
| Credit | Aumentar pontos disponíveis. Crédito comum registra pontos ganhos; transferência recebida também gera um lançamento de crédito. |
| Debit | Reduzir pontos disponíveis. Débito comum registra resgate; transferência enviada também gera um lançamento de débito. |
| Balance | Pontos de recompensa atualmente disponíveis. |
| Transfer | Move atomicamente 1..100 pontos entre carteiras distintas, com saldo suficiente na origem e dois lançamentos vinculados; não cria nem destrói pontos. |
| Source Wallet / Destination Wallet | Carteira que fornece / recebe os pontos transferidos. |
| Reward Operation | Crédito, débito ou transferência aceito, identificado globalmente pelo operationId do cliente. |
| Ledger Entry | Evidência imutável por carteira: um lançamento para movimento comum; dois vinculados para transferência. transactionId é interno; operationId identifica a operação de negócio. |
| Can Afford | Verificar se o saldo cobre os pontos necessários sem alterar o estado. |
| Suspend | Bloquear temporariamente resgates, mantendo o recebimento de pontos ganhos. |
| Reactivate | Devolver uma carteira suspensa ao uso ativo. |
| Close | Impedir permanentemente novos créditos e resgates. |
| Reward Transaction | Registro imutável de um crédito ou débito, identificado por operationId e explicado por um motivo. |
| Idempotent Retry | Repetir o mesmo comando com o mesmo operationId e os mesmos dados sem movimentar pontos novamente. |

A aplicação gera um ID não vazio e registra o instante de criação em UTC. O nome é normalizado e deve ter entre 1 e 120 caracteres. Novas carteiras começam ativas e sem pontos. Carteiras suspensas aceitam créditos, mas bloqueiam débitos e a capacidade de resgate; carteiras encerradas rejeitam toda movimentação. Crédito e débito exigem ao menos um ponto, um motivo de até 200 caracteres e um operationId não vazio. Reutilizar esse ID com dados diferentes retorna conflito; reutilizá-lo com os mesmos dados devolve o resultado original.

RewardPoints usa long e aceita valores de zero a long.MaxValue. Construção negativa, subtração abaixo de zero e soma acima do limite produzem exceções de domínio. Operações rejeitadas preservam os valores originais e o saldo da carteira.

## Como uma requisição percorre o projeto

O diagrama abaixo representa o crédito atual. A sequência da transferência entre duas carteiras, incluindo rollback, está no [plano](docs/transfer-plan.md#sequence-and-atomicity).

A operação de crédito mostra como os adaptadores e o núcleo colaboram sem transferir regras de negócio para o controller ou para o repositório:

~~~mermaid
sequenceDiagram
    actor Client
    participant API as WalletsController
    participant UseCase as ICreditRewardPoints
    participant Repository as IRewardsWalletRepository
    participant Wallet as RewardsWallet
    participant Points as RewardPoints

    Client->>API: POST credit (operationId, points, reason)
    API->>UseCase: ExecuteAsync(id, operationId, points, reason)
    UseCase->>Repository: FindOperationKindAsync / FindTransactionAsync(operationId)
    UseCase->>Repository: FindAsync(id)
    Repository-->>UseCase: detached WalletSnapshot
    UseCase->>Wallet: Credit(earnedPoints)
    Wallet->>Points: Add(earnedPoints)
    Points-->>Wallet: new RewardPoints
    UseCase->>Repository: TrySaveMovementAsync(wallet, transaction, revision)
    Repository-->>UseCase: balance and movement saved atomically
    UseCase-->>API: RewardMovement
    API-->>Client: 200 RewardMovementResponse
~~~

1. WalletsController valida e traduz a requisição HTTP, depois chama a porta de entrada da aplicação.
2. CreditRewardPoints coordena o caso de uso e pede à porta de repositório um snapshot desacoplado.
3. RewardsWallet.Credit expressa a mudança na linguagem do domínio e delega a aritmética imutável a RewardPoints.Add.
4. A aplicação cria uma RewardTransaction e salva saldo e registro usando a revisão carregada na mesma transação do banco. Transferências exigem as duas revisões e confirmam ambas as carteiras e os dois lançamentos juntos; revisão obsoleta retorna 409 e reverte tudo.
5. Se o operationId já existir com os mesmos dados, o resultado original é devolvido sem alterar o saldo novamente.
6. O caso de uso retorna um resultado de aplicação, convertido pelo controller em um DTO de resposta.

Débito segue o mesmo fluxo de comando. Saldo, capacidade de pagamento e extrato são consultas sem mutação. Falhas conhecidas chegam a ApiExceptionHandler, que as converte em HTTP Problem Details na fronteira externa.

## Arquitetura: Clean Architecture e Portas Hexagonais

O diagrama abaixo mostra o sistema implementado. A [arquitetura da transferência](docs/transfer-plan.md#architecture) acrescenta cálculo de domínio e uma capacidade específica de persistência atômica, sem novas camadas arquiteturais.

~~~mermaid
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
        InboundPorts["Inbound Ports<br/>IOpenRewardsWallet<br/>ICreditRewardPoints<br/>ITransferRewardPoints<br/>IDebitRewardPoints<br/>IGetWalletBalance<br/>ICheckWalletAffordability"]
        UseCases["Use Case Implementations<br/>Transfer - Open · Credit · Debit · Balance · Affordability"]
        InboundPorts --> UseCases
    end

    subgraph Domain["Domain · Business Core"]
        direction TB
        WalletPort["IRewardsWallet<br/>Credit · Debit · CanAfford"]
        Wallet["RewardsWallet<br/>Aggregate"]
        Points["RewardPoints + WalletName + TransferAmount<br/>Immutable Value Objects"]
        Rules["DomainAssertions<br/>DomainException"]
        RepositoryPort["IRewardsWalletRepository<br/>Outbound Port"]

        Calculator["RewardTransferCalculator<br/>Two-wallet calculation"]
        UseCases --> Calculator
        Calculator --> Wallet
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
    class Wallet,Points,Rules,Calculator domain
    class InboundPorts,WalletPort,RepositoryPort port
~~~

As setas contínuas mostram o fluxo das requisições e a colaboração no domínio. As tracejadas mostram a composição dos adaptadores: Program.cs escolhe as implementações e Infrastructure implementa a porta de repositório pertencente ao Domain.

Domain não possui referências a outros projetos ou pacotes. Application e Infrastructure referenciam Domain de forma independente. Infrastructure não conhece HTTP, Application não referencia Infrastructure e os objetos do domínio nunca são serializados diretamente como contratos HTTP.

## Estrutura do projeto

Os arquivos de transferência e suas verificações estão no [guia de implementação](docs/transfer-plan.md#implementation-sequence-and-acceptance-criteria).

~~~text
RewardFlow.SuppleDesign/
├── RewardFlow.SuppleDesign.slnx
├── RewardFlow.SuppleDesign.slnLaunch              # Optional shared startup profile
├── Directory.Build.props                         # Shared .NET 8 build settings
├── global.json                                   # Reproducible SDK feature band
├── compose.yaml                                  # Local PostgreSQL 17 container and volume
├── dotnet-tools.json                             # Reproducible EF Core CLI version
├── docs/decisions/                               # Architectural decision records
├── README.md
│
├── src/                                           # Production code
│   ├── RewardFlow.SuppleDesign.Domain/            # Business model; no external dependencies
│   │   ├── Entities/
│   │   │   ├── RewardsWallet.cs                   # Aggregate root and lifecycle rules
│   │   │   ├── RewardsWalletStatus.cs
│   │   │   ├── RewardTransfer.cs                  # Original paired-operation result
│   │   │   └── RewardTransaction.cs               # Identified immutable movement
│   │   ├── Services/RewardTransferCalculator.cs     # Calculation without mutation
│   │   ├── ValueObjects/
│   │   │   ├── TransferAmount.cs                  # Transfer-specific 1..100 rule
│   │   │   ├── RewardPoints.cs                    # Immutable arithmetic value object
│   │   │   ├── WalletName.cs                      # Normalized name value object
│   │   │   └── RewardReason.cs                    # Normalized movement reason
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
│   │   │   ├── TransferPoints/TransferRewardPoints.cs
│   │   │   ├── CreditPoints/
│   │   │   ├── DebitPoints/
│   │   │   ├── ChangeStatus/
│   │   │   ├── GetWallet/
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
│       │       ├── WalletContracts.cs
│       │       └── WalletContractExamples.cs
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
        ├── TransferDomainTests.cs
        ├── TransferApplicationTests.cs
        ├── TransferPostgresTests.cs
        ├── TransferApiTests.cs
        ├── TransferMigrationTests.cs
        ├── DomainTests.cs
        ├── ApplicationTests.cs
        ├── PostgresRepositoryTests.cs
        ├── WalletApiTests.cs
        └── RewardFlow.SuppleDesign.Tests.csproj
~~~

## Blocos de construção de Evans e o RewardFlow

A transferência usa um serviço de domínio justificado pelo cálculo entre duas carteiras, separado da coordenação da aplicação. Consulte o [ADR-003](docs/decisions/003-atomic-wallet-transfers.md).

A figura do livro apresenta os blocos mais amplos de **Model-Driven Design** de Evans. Ela complementa o Capítulo 10, mas não é um diagrama dos padrões de Supple Design: Entities, Value Objects, Aggregates, Repositories, Services e Factories são blocos táticos; Intention-Revealing Interfaces, Side-Effect-Free Functions, Assertions, Conceptual Contours, Standalone Classes e Closure of Operations tornam esse modelo flexível.

~~~mermaid
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
        Calculator["RewardTransferCalculator<br/>Domain service"]
        UseCases["Application Use Cases<br/>Open · Credit · Debit · Queries · Lifecycle"]
        Construction["Constructors<br/>Simple creation; no Factory needed"]
        Layers["Domain · Application<br/>Infrastructure · API"]
        Controllers["Thin Controllers<br/>HTTP concerns only"]
    end

    E --> Wallet
    A --> Wallet
    V --> Points
    R --> Repository
    S --> Calculator
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
~~~

| Bloco de DDD | Implementação | Decisão |
| --- | --- | --- |
| Entity | RewardsWallet | Possui identidade estável e comportamento que altera seu ciclo de vida. |
| Value Object | RewardPoints e WalletName | Não têm identidade, comparam por valor e protegem suas próprias regras. |
| Aggregate | RewardsWallet | É a fronteira de consistência para identidade e saldo. |
| Repository | IRewardsWalletRepository e adaptador PostgreSQL | A porta pertence ao domínio e a tecnologia fica na infraestrutura. |
| Service | RewardTransferCalculator | Relaciona duas carteiras usando consultas de saldo sem mutação; os casos de uso coordenam I/O separadamente. |
| Factory | Construtores | A criação é simples; uma Factory não protegeria uma nova invariante. |
| Layered Architecture | Quatro projetos | Domain isolado, Application coordenando e Infrastructure/API como adaptadores. |
| Smart UI | Ausente deliberadamente | Controllers traduzem HTTP e não contêm regras de recompensa. |

Os diagramas Mermaid são interpretações específicas deste projeto, não reproduções de figuras do livro.

## Como o código aplica os padrões de Supple Design de Evans

| Padrão | Código | Por que importa |
| --- | --- | --- |
| Intention-Revealing Interfaces | Credit, Debit, CanAfford, Suspend, Reactivate, Close e portas nomeadas | O código usa o vocabulário do domínio em vez de setters ou métodos genéricos. |
| Side-Effect-Free Functions | RewardsWallet.CanAfford; RewardPoints.Add e Subtract | Consultas não alteram estado e cálculos devolvem novos valores imutáveis. |
| Assertions | DomainAssertions, Value Objects, limites de pontos e transições de status | Pré-condições e invariantes são verificadas em runtime. |
| Conceptual Contours | Value Object, agregado, casos de uso, controllers e repositório | Cada conceito possui uma razão clara e diferente para mudar. |
| Standalone Classes | RewardPoints e WalletName | Reúnem valor e validade sem depender de ORM, DI ou framework web. |
| Closure of Operations | RewardPoints.Add(RewardPoints) e Subtract(RewardPoints) | Operandos e resultados pertencem ao mesmo conjunto conceitual. |

Por exemplo, new RewardPoints(100).Add(new RewardPoints(25)).Subtract(new RewardPoints(40)) produz 85 pontos sem alterar nenhum operando. RewardsWallet.Credit delega essa aritmética e só substitui o saldo depois de uma validação bem-sucedida.

## Orçamento de abstrações

As abstrações de transferência têm responsabilidades concretas: `TransferAmount` protege 1..100; `RewardTransferCalculator` calcula os dois resultados sem mutação; a evidência de transferência registra a operação pareada. O registro global de operações separa idempotência da identidade dos lançamentos. A justificativa e as alternativas estão no [plano](docs/transfer-plan.md); estão implementadas.

| Abstração | Por que existe | Quando reconsiderar |
| --- | --- | --- |
| RewardPoints | Centraliza aritmética, limites e igualdade de valor. | Se pontos deixarem de ter regras além do tipo primitivo. |
| WalletName | Centraliza normalização, obrigatoriedade e limite de tamanho. | Se o nome deixar de possuir regras além de texto livre. |
| RewardsWallet | Define a fronteira de consistência da carteira. | Se o comportamento se dividir em conceitos independentes. |
| IRewardsWallet | Expõe um contrato pequeno com linguagem reveladora de intenção. | Se o objetivo didático deixar de exigir o papel explícito. |
| DomainAssertions | Compartilha invariantes com códigos de erro do domínio. | Se uma regra pertencer a somente um tipo. |
| Portas de casos de uso | Separam o adaptador HTTP da orquestração concreta. | Se não existir mais uma fronteira de adaptadores. |
| IRewardsWalletRepository | Permite que PostgreSQL e test double obedeçam ao mesmo contrato. | Se o armazenamento deixar de ser substituível. |
| WalletSnapshot e revisão | Impedem a sobrescrita silenciosa por comandos concorrentes. | Se o armazenamento oferecer contrato equivalente mais simples. |
| RewardFlowDbContext e modelos | Isolam EF Core e o modelo relacional do domínio. | Se outro adaptador substituir o EF Core. |
| DTOs e ApiExceptionHandler | Mantêm serialização, status e Problem Details na API. | Se o adaptador primário mudar. |

O projeto não contém generic repository, wrapper de Unit of Work, mediator, framework de mapeamento, factory, domain service ou event bus. Nenhum deles protege atualmente uma invariante adicional ou atende a um segundo adaptador.

## Decisões arquiteturais

Decisões cujos trade-offs atravessam várias classes estão registradas em [`docs/decisions`](docs/decisions/README.md). Os registros atuais explicam por que o extrato permanece fora da reconstituição da carteira, como `operationId` fornece idempotência, por que saldo e movimentação são confirmados atomicamente e por que esta API educacional aplica migrations no startup.

Os registros preservam o raciocínio para que o leitor diferencie uma fronteira conceitual deliberada de uma estrutura incidental da implementação.

## Contrato HTTP e erros

A tabela descreve rotas implementadas. O contrato de `POST /api/wallets/{sourceWalletId}/transfers`, seus exemplos e erros estão no [plano](docs/transfer-plan.md#http-contract). O endpoint está disponível.

| Método | Rota | Body | Sucesso |
| --- | --- | --- | --- |
| POST | /api/wallets | {"name":"Recompensas principais"} | 201, ID gerado e localização da carteira |
| GET | /api/wallets/{id} | nenhum | 200 e dados da carteira |
| POST | /api/wallets/{id}/credit | operationId, pontos e motivo | 200 e movimentação aceita |
| POST | /api/wallets/{id}/debit | operationId, pontos e motivo | 200 e movimentação aceita |
| POST | `/api/wallets/{sourceWalletId}/transfers` | operationId, destinationWalletId, points (1..100), reason | 200 com resultado original |
| GET | /api/wallets/{id}/transactions?page=1&pageSize=20 | Sem body | 200 e página do extrato |
| GET | /api/wallets/{id}/balance | nenhum | 200 e saldo atual |
| GET | /api/wallets/{id}/can-afford?requiredPoints=60 | nenhum | 200 e resultado da consulta |
| POST | /api/wallets/{id}/suspend | nenhum | 204 após suspensão |
| POST | /api/wallets/{id}/reactivate | nenhum | 204 após reativação |
| POST | /api/wallets/{id}/close | nenhum | 204 após encerramento definitivo |

A criação devolve apenas `{"walletId":"<guid>"}`; o header `Location` endereça a nova carteira. Comandos e consultas de saldo devolvem `{"points":60}`, pois o ID já aparece na rota.

| Status | Situação |
| --- | --- |
| 400 | JSON inválido, nome ausente, vazio ou longo demais, ou pontos ausentes, fracionários, não positivos ou fora do intervalo de long. |
| 404 | Carteira com ID bem-formado não encontrada. |
| 409 | Escrita concorrente obsoleta ou comando incompatível com o status atual. |
| 422 | Violação de regra, como pontos insuficientes, overflow ou GUID vazio. |
| 500 | Falha inesperada sem exposição de detalhes internos. |

Falhas esperadas usam application/problem+json e incluem um code estável:

~~~json
{
  "title": "Reward rule violated",
  "status": 422,
  "detail": "The rewards wallet cannot afford the required points.",
  "code": "insufficient_points"
}
~~~

## Persistência e concorrência

PostgresRewardsWalletRepository implementa a porta pertencente ao domínio usando EF Core e Npgsql. O adaptador converte RewardsWalletState, exclusivo da persistência, em novos objetos RewardsWallet e RewardPoints a cada leitura. O agregado não possui atributos do EF Core, proxies, propriedades de navegação ou construtor voltado à persistência.

Uma carteira é inserida com revisão 0 e a chave primária protege a identidade gerada. Ao aceitar uma movimentação, o adaptador atualiza o saldo pela revisão esperada e insere a transação na mesma transação do PostgreSQL. A chave de `operation_id` impede duplicidade, enquanto uma revisão obsoleta impede perda de atualização. Uma repetição idêntica devolve o registro original; dados diferentes para o mesmo identificador retornam 409.

A connection string é lida de ConnectionStrings:RewardFlow. Em outros ambientes, use ConnectionStrings__RewardFlow. A senha versionada serve apenas para desenvolvimento local.

### Modelo de dados (DER)

O esquema implementado contém `reward_operations`, `reward_transfers` e `transaction_id` independente no extrato, permitindo dois lançamentos para uma operação sem perder os IDs antigos.

~~~mermaid
erDiagram
    REWARDS_WALLETS {
        uuid id PK
        varchar name
        integer status
        timestamptz created_at_utc
        bigint points
        bigint revision
    }
    REWARD_OPERATIONS {
        uuid operation_id PK
        integer kind "Credit, Debit, Transfer"
    }
    REWARD_TRANSFERS {
        uuid operation_id PK,FK
        uuid source_wallet_id FK
        uuid destination_wallet_id FK
        bigint points "1..100"
        varchar reason "1..200 normalized"
        timestamptz occurred_at_utc
        bigint source_balance_after
        bigint destination_balance_after
    }
    REWARD_TRANSACTIONS {
        uuid transaction_id PK
        uuid operation_id FK
        uuid wallet_id FK
        integer type "Credit or Debit"
        bigint points
        varchar reason
        timestamptz occurred_at_utc
        bigint balance_after
    }
    REWARD_OPERATIONS ||--o| REWARD_TRANSFERS : identifies
    REWARD_OPERATIONS ||--|{ REWARD_TRANSACTIONS : records
    REWARDS_WALLETS ||--o{ REWARD_TRANSACTIONS : has
    REWARDS_WALLETS ||--o{ REWARD_TRANSFERS : sends
    REWARDS_WALLETS ||--o{ REWARD_TRANSFERS : receives
~~~

WalletName, RewardReason e RewardPoints são armazenados de forma achatada. `revision` pertence à coordenação da persistência, não ao agregado. Constraints rejeitam nomes e motivos vazios, status e tipos desconhecidos, pontos inválidos, saldos negativos e revisões negativas.

### Migrations

**Implementada:** `20261024215631_AddWalletTransfers`. O [plano de migration](docs/transfer-plan.md#migration-addwallettransfers) detalha backfill, chaves/FKs, constraints, compatibilidade dos endpoints existentes, testes de upgrade, restrições de rollback e rollout. A migration, seu designer e o snapshot atualizado estão versionados. O backfill preserva IDs antigos e o downgrade é bloqueado quando existem transferências. Pare escritores da versão anterior antes de atualizar.

As migrations estão versionadas em Infrastructure/Persistence/Migrations, incluindo as evoluções de perfil, ciclo de vida e extrato. Antes de ativar o middleware, Program.cs chama ApplyRewardFlowMigrationsAsync, que executa Database.MigrateAsync. Quando o PostgreSQL não está acessível, a inicialização informa diretamente o comando `docker compose up -d --wait`.

Para criar uma migration após alterar o mapeamento:

~~~powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add DescrevaAAlteracaoDoEsquema `
    --project src/RewardFlow.SuppleDesign.Infrastructure/RewardFlow.SuppleDesign.Infrastructure.csproj `
    --output-dir Persistence/Migrations
~~~

> [!CAUTION]
> A migration em runtime é intencional neste projeto educacional autocontido. Em produção, revise as migrations e prefira um script SQL ou migration bundle executado pelo processo de deployment com permissões específicas para alterar o esquema.

~~~powershell
# Interrompe os containers e preserva os dados.
docker compose down

# Exclui o volume local e reinicia com um banco vazio.
docker compose down --volumes
~~~

## Testes

A suíte Release passa 70 testes, incluindo [verificações de transferência](docs/transfer-plan.md#implementation-sequence-and-acceptance-criteria): domínio, HTTP, revisões de duas carteiras, concorrência, rollback com falhas injetadas e preservação de histórico na migration. Isso não implica cobertura de todos os entrelaçamentos possíveis.

O teste de migration cria e remove seu próprio banco temporário; o usuário configurado precisa de CREATEDB, disponível no usuário do Compose local.

A suíte xUnit cobre:

- Igualdade por valor, aritmética imutável, limites, saldo insuficiente e overflow.
- Normalização do nome, identidade gerada, criação em UTC e transições do ciclo de vida.
- Resgate do saldo exato, proteção do saldo zero, identidade vazia e preservação do estado em falhas.
- Consultas de capacidade de pagamento sem mutação.
- Orquestração, snapshots desacoplados e rejeição de escritas obsoletas.
- Idempotência de movimentações, conflito por reutilização incorreta do identificador e extrato paginado.
- Repetições idênticas concorrentes convergindo em uma movimentação e rollback quando o registro do extrato entra em conflito após a atualização do saldo.
- Paginação do extrato e `hasNextPage`. A implementação busca um item adicional em vez de consultar a contagem; a suíte verifica resultados da paginação, sem instrumentar a quantidade de consultas SQL.
- Mapeamento PostgreSQL, migration no startup e atualização condicional por revisão. A persistência entre reinícios decorre do armazenamento PostgreSQL; não há teste dedicado de reinício da API.
- Rotas e DI reais pelo WebApplicationFactory, contratos, status HTTP e Problem Details.
- Exemplos claros no Swagger, independentes da implementação da validação HTTP.

Inicie o banco com docker compose up -d --wait antes de executar dotnet test RewardFlow.SuppleDesign.slnx. Os testes da aplicação usam o repositório em memória somente como test double rápido do mesmo contrato.

## Melhorias educacionais futuras

Cada evolução futura deve introduzir uma pressão concreta de modelagem e tornar visível seu efeito sobre o design. O objetivo é aprofundar o exemplo de Supple Design sem transformar o repositório em um catálogo de padrões de infraestrutura.

| Evolução para aprendizado | Pergunta de domínio a explorar | Foco em Supple Design |
| --- | --- | --- |
| Expiração de pontos | Como pontos ganhos em momentos diferentes devem expirar sem quebrar as regras de resgate? | Criar conceitos temporais explícitos, Intention-Revealing Interfaces e Assertions, evitando comparações de datas espalhadas pelos casos de uso. |
| Política configurável de acúmulo | Como valor da compra, campanha e categoria do membro são convertidos em pontos? | Modelar o cálculo como uma Standalone Class com uma Side-Effect-Free Function, entradas explícitas e resultado previsível. |
| Exemplos algébricos e testes baseados em propriedades | Quais propriedades sempre devem valer nas operações com pontos? | Demonstrar Closure of Operations, imutabilidade e invariantes como identidade aditiva e rejeição de resultados negativos. |

Cada incremento deve começar com exemplos, vocabulário e testes que expressem a nova necessidade. Somente depois disso o modelo deve mudar. O README e o orçamento de abstrações também devem registrar a decisão, as alternativas consideradas e a condição que justifica cada novo tipo ou fronteira.

## Fontes e leituras complementares

Este projeto foi inspirado por *Domain-Driven Design: Tackling Complexity in the Heart of Software*, de Eric Evans (Addison-Wesley, 2003, ISBN 978-0-321-12521-7).

> [!IMPORTANT]
> A referência central é a **Parte III, Capítulo 10 — “Supple Design”**. O capítulo conecta modelagem profunda a código claro, previsível e adequado à refatoração contínua.

| Recurso | Finalidade |
| --- | --- |
| [Página oficial do livro — InformIT](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217) | Descrição, publicação e opções de compra |
| [Capítulo 10: Supple Design — O'Reilly Learning](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) | Catálogo e leitura do capítulo; pode exigir assinatura ou compra |
| [DDD Reference — Domain Language](https://www.domainlanguage.com/ddd/reference/) | Resumo gratuito de definições e padrões por Eric Evans |
| [Provider Npgsql para EF Core](https://www.npgsql.org/efcore/) | Configuração do PostgreSQL e integração com EF Core |
| [Aplicação de migrations do EF Core](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) | Estratégias de deployment e implicações de migrations em runtime |

## Aviso sobre os comentários educacionais

> [!NOTE]
> Este repositório possui intencionalmente mais comentários que um código típico de produção. A documentação XML identifica o padrão demonstrado, explica decisões e conecta a implementação aos conceitos de DDD. Em produção, comentários normalmente devem explicar intenção e trade-offs que não sejam evidentes no próprio código.

| Código | Foco educacional | Referência |
| --- | --- | --- |
| [RewardPoints](src/RewardFlow.SuppleDesign.Domain/ValueObjects/RewardPoints.cs) | Standalone Class, Closure of Operations e Side-Effect-Free Functions | [Capítulo 10](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [WalletName](src/RewardFlow.SuppleDesign.Domain/ValueObjects/WalletName.cs) | Standalone Class e Assertions | [Capítulo 10](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [RewardReason](src/RewardFlow.SuppleDesign.Domain/ValueObjects/RewardReason.cs) | Standalone Class e linguagem explícita para o motivo | [Capítulo 10](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [RewardTransaction](src/RewardFlow.SuppleDesign.Domain/Entities/RewardTransaction.cs) | Entity e novo Conceptual Contour para auditoria e idempotência | [DDD Reference](https://www.domainlanguage.com/ddd/reference/) |
| [IRewardsWallet](src/RewardFlow.SuppleDesign.Domain/Ports/IRewardsWallet.cs) | Intention-Revealing Interfaces | [Capítulo 10](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [DomainAssertions](src/RewardFlow.SuppleDesign.Domain/DomainAssertions.cs) | Assertions e invariantes explícitas | [Capítulo 10](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [RewardsWallet](src/RewardFlow.SuppleDesign.Domain/Entities/RewardsWallet.cs) | Conceptual Contours, Entity e Aggregate Root | [DDD Reference](https://www.domainlanguage.com/ddd/reference/) |
| [IRewardsWalletRepository](src/RewardFlow.SuppleDesign.Domain/Ports/IRewardsWalletRepository.cs) | Repository como porta de saída do domínio | [DDD Reference](https://www.domainlanguage.com/ddd/reference/) |

## Documentação, skills e guardrails

O propósito principal é implementar os padrões de Supple Design de Eric Evans de forma objetiva e educacional. A [constitution](docs/constitution.md) define inglês como idioma canônico dos documentos, planos, ADRs, skills, diagramas, código e comentários; este README é a exceção explícita como tradução para português. Consulte o [índice da documentação](docs/README.md), o [guia dos padrões](docs/supple-design.md), a [análise do projeto](docs/project-analysis.md) e os [guardrails](docs/guardrails.md). O exemplo demonstra seis padrões; não reivindica cobertura integral do capítulo 10, incluindo design declarativo. Intention-Revealing Interfaces trata de contratos públicos que comunicam intenção e não exige uma interface C# para cada tipo.

| Recurso | Papel |
| --- | --- |
| [AGENTS.md](AGENTS.md) | Instruções do repositório e guardrails essenciais para agentes compatíveis |
| [rewardflow-evolve](.agents/skills/rewardflow-evolve/SKILL.md) | Procedimento para evoluir regras, value objects e casos de uso |
| [rewardflow-review](.agents/skills/rewardflow-review/SKILL.md) | Procedimento de revisão de correção e explicações educacionais |
| [Guardrails](docs/guardrails.md) | Critérios de revisão para pessoas e agentes |
| [CI](.github/workflows/ci.yml) | Restore, build Release e testes com PostgreSQL automatizados |

No Codex, as instruções são descobertas pelo `AGENTS.md`; as skills em `.agents/skills/` podem ser selecionadas automaticamente pela descrição ou invocadas com `$rewardflow-evolve` e `$rewardflow-review`. Normalmente não é necessário anunciar sua existência em cada prompt. A descoberta depende da ferramenta e da configuração; se uma skill não estiver disponível, indique ao agente o `SKILL.md` correspondente para leitura direta. Consulte a documentação oficial da OpenAI sobre [AGENTS.md](https://developers.openai.com/codex/guides/agents-md) e [skills](https://developers.openai.com/codex/skills).

Pessoas devem consultar essas instruções ao contribuir; outras ferramentas podem exigir configuração ou contexto explícito. As instruções orientam mudanças; testes e CI verificam apenas os critérios implementados. Clareza educacional e precisão conceitual continuam dependendo de revisão. Skills não concedem autorização para publicar ou executar ações externas sem relação com a tarefa.

Consultas de capacidade de resgate em carteiras suspensas ou encerradas retornam `false`. Créditos e débitos retornam a movimentação aceita com `balanceAfter`; a consulta de saldo retorna `points`. Uma repetição idêntica de operação aceita devolve seu resultado original, inclusive após mudança do estado da carteira, sem realizar nova movimentação.

## Como contribuir

Leia os [guardrails](docs/guardrails.md) antes de alterar comportamentos. Mantenha código-fonte, identificadores e comentários de código em inglês. Alinhe afirmações compartilhadas nas duas versões do README. Gere um `operationId` globalmente único para cada nova movimentação, inclusive ao repetir o exemplo `.http` com outra carteira; preserve esse ID somente ao repetir a mesma operação.

RewardFlow.SuppleDesign é um projeto colaborativo. Melhorias, críticas construtivas e sugestões são sempre bem-vindas.

1. Faça um fork do repositório.
2. Crie uma branch que descreva a mudança.
3. Implemente a melhoria mantendo explícitas as fronteiras arquiteturais.
4. Execute dotnet test RewardFlow.SuppleDesign.slnx.
5. Abra um pull request explicando a motivação, o comportamento resultante e a validação realizada.

Contribuições podem incluir correções de texto, exemplos mais claros, novos adaptadores, testes mais fortes e outras demonstrações de Supple Design. Se algo parecer confuso ou você discordar de uma decisão, abra uma discussão ou um pull request.

---

<div align="center">

Construído como um estudo colaborativo de modelagem de domínio e design de software flexível.

[Voltar ao início](#inicio)

</div>
