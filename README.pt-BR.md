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
- [Contrato HTTP e erros](#contrato-http-e-erros)
- [Persistência e concorrência](#persistência-e-concorrência)
- [Modelo de dados (DER)](#modelo-de-dados-der)
- [Migrations](#migrations)
- [Testes](#testes)
- [Melhorias educacionais futuras](#melhorias-educacionais-futuras)
- [Fontes e leituras complementares](#fontes-e-leituras-complementares)
- [Aviso sobre os comentários educacionais](#aviso-sobre-os-comentários-educacionais)
- [Como contribuir](#como-contribuir)

## O que é o RewardFlow?

O **RewardFlow** é um pequeno sistema de gestão de fidelidade e pontos de recompensa. Cada membro é representado por uma carteira de recompensas, que pode ser aberta, receber créditos quando pontos são ganhos, receber débitos quando pontos são resgatados, informar o saldo atual e verificar se possui pontos suficientes para um resgate.

O projeto se concentra nesse fluxo de negócio compacto para manter visíveis a linguagem do domínio e as decisões de design. Ele é um modelo educacional, não uma plataforma completa de fidelidade: cadastro de membros, campanhas, expiração de pontos, autenticação e livro-razão de transações estão fora do escopo atual.

## Avaliação de Supple Design

> [!IMPORTANT]
> **O RewardFlow atende ao propósito de Supple Design dentro do escopo de um exemplo educacional.** O modelo fala a linguagem de recompensas, protege as invariantes onde o estado reside, mantém consultas e cálculos previsíveis e isola as decisões de negócio de HTTP e persistência.

A flexibilidade vem de conceitos explícitos e fronteiras substituíveis. O PostgreSQL é um adaptador secundário, enquanto o EF Core permanece fora dos projetos Domain e Application. O exemplo não adiciona factories, domain services, mensageria ou outros mecanismos sem uma necessidade concreta de modelagem. O [mapeamento dos padrões](#como-o-código-aplica-os-padrões-de-supple-design-de-evans) mostra as evidências no código, e o [orçamento de abstrações](#orçamento-de-abstrações) explica por que cada fronteira existe.

---

## Executar e verificar

### Pré-requisitos

Instale o Docker Desktop, um SDK do .NET com suporte a arquivos .slnx — versão 9.0.200 ou superior — e o runtime do ASP.NET Core 8. O primeiro restore precisa de acesso ao NuGet para obter EF Core, Npgsql, pacotes de teste, Swagger e, quando necessário, os targeting packs do .NET 8. Domain e Application continuam sem dependências de pacotes de terceiros.

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

O Compose inicia uma imagem fixada do PostgreSQL com as credenciais locais de desenvolvimento definidas em compose.yaml. Antes de aceitar requisições, a API aplica as migrations pendentes do EF Core. Na primeira execução, o banco e a tabela são criados automaticamente. As carteiras permanecem disponíveis depois que a API é reiniciada porque o PostgreSQL usa um volume Docker nomeado.

## Como realizar ações na carteira

Inicie a API e escolha uma destas opções: execute as requisições pelo Swagger em [http://localhost:5180/](http://localhost:5180/), abra [RewardFlow.http](src/RewardFlow.SuppleDesign.Api/RewardFlow.http) em um cliente HTTP compatível ou siga o exemplo em PowerShell.

Use o mesmo identificador durante todo o fluxo. Uma carteira precisa ser aberta antes de receber qualquer comando ou consulta.

| Objetivo | Requisição | Entrada | Resultado esperado |
| --- | --- | --- | --- |
| Abrir uma carteira | POST /api/wallets | { "name": "Recompensas principais" } | 201 Created, walletId gerado e header Location |
| Consultar a carteira | GET /api/wallets/{id} | Sem body | 200 OK com nome, status, criação e saldo |
| Creditar pontos ganhos | POST /api/wallets/{id}/credit | { "points": 100 } | 200 OK com o saldo atualizado |
| Debitar pontos resgatados | POST /api/wallets/{id}/debit | { "points": 40 } | 200 OK, se houver saldo suficiente |
| Consultar o saldo | GET /api/wallets/{id}/balance | Sem body | 200 OK com os pontos atuais |
| Verificar capacidade de pagamento | GET /api/wallets/{id}/can-afford?requiredPoints=60 | Pontos inteiros não negativos | 200 OK com canAfford; o saldo não muda |
| Suspender / reativar / encerrar | POST /api/wallets/{id}/{ação} | Sem body | 204 No Content após uma transição válida |

<details>
<summary><strong>PowerShell: abrir, creditar, debitar e consultar uma carteira</strong></summary>

<br />

~~~powershell
$baseUri = 'http://localhost:5180'
# A API gera a identidade e a devolve com 201 Created.
$opened = Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets" `
    -ContentType 'application/json' -Body '{"name":"Recompensas principais"}'
$walletId = $opened.walletId

Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId"

# Ganha 100 pontos: 200 OK.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/credit" `
    -ContentType 'application/json' -Body '{"points":100}'

# Resgata 40 pontos: 200 OK, restam 60.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/debit" `
    -ContentType 'application/json' -Body '{"points":40}'

Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId/balance"

# Consulta pura: true, sem alterar os 60 pontos.
Invoke-RestMethod -Uri "$baseUri/api/wallets/$walletId/can-afford?requiredPoints=60"

# Rejeitado: 422 Unprocessable Entity; o saldo permanece em 60.
Invoke-RestMethod -Method Post -Uri "$baseUri/api/wallets/$walletId/debit" `
    -ContentType 'application/json' -Body '{"points":61}'
~~~

</details>

## Linguagem Ubíqua e regras

| Termo | Significado |
| --- | --- |
| Reward Points | Unidades inteiras e não negativas, ganhas e resgatadas por um membro; representadas por RewardPoints. |
| Rewards Wallet | Agregado que mantém os pontos disponíveis de um membro identificado. |
| Open | Criar uma carteira identificada com zero pontos. |
| Credit | Adicionar pontos ganhos a uma carteira existente. |
| Debit | Remover pontos resgatados de uma carteira existente. |
| Balance | Pontos de recompensa atualmente disponíveis. |
| Can Afford | Verificar se o saldo cobre os pontos necessários sem alterar o estado. |
| Suspend | Bloquear temporariamente resgates, mantendo o recebimento de pontos ganhos. |
| Reactivate | Devolver uma carteira suspensa ao uso ativo. |
| Close | Impedir permanentemente novos créditos e resgates. |

A aplicação gera um ID não vazio e registra o instante de criação em UTC. O nome é normalizado e deve ter entre 1 e 120 caracteres. Novas carteiras começam ativas e sem pontos. Carteiras suspensas aceitam créditos, mas bloqueiam débitos e a capacidade de resgate; carteiras encerradas rejeitam toda movimentação. Crédito e débito exigem ao menos um ponto, e é permitido debitar exatamente o saldo disponível.

RewardPoints usa long e aceita valores de zero a long.MaxValue. Construção negativa, subtração abaixo de zero e soma acima do limite produzem exceções de domínio. Operações rejeitadas preservam os valores originais e o saldo da carteira.

## Como uma requisição percorre o projeto

A operação de crédito mostra como os adaptadores e o núcleo colaboram sem transferir regras de negócio para o controller ou para o repositório:

~~~mermaid
sequenceDiagram
    actor Cliente
    participant API as WalletsController
    participant CasoDeUso as ICreditRewardPoints
    participant Repositorio as IRewardsWalletRepository
    participant Carteira as RewardsWallet
    participant Pontos as RewardPoints

    Cliente->>API: POST /api/wallets/{id}/credit
    API->>CasoDeUso: ExecuteAsync(id, points)
    CasoDeUso->>Repositorio: FindAsync(id)
    Repositorio-->>CasoDeUso: WalletSnapshot desacoplado
    CasoDeUso->>Carteira: Credit(earnedPoints)
    Carteira->>Pontos: Add(earnedPoints)
    Pontos-->>Carteira: novo RewardPoints
    CasoDeUso->>Repositorio: TrySaveAsync(wallet, revision)
    Repositorio-->>CasoDeUso: salvo
    CasoDeUso-->>API: WalletBalance
    API-->>Cliente: 200 WalletBalanceResponse
~~~

1. WalletsController valida e traduz a requisição HTTP, depois chama a porta de entrada da aplicação.
2. CreditRewardPoints coordena o caso de uso e pede à porta de repositório um snapshot desacoplado.
3. RewardsWallet.Credit expressa a mudança na linguagem do domínio e delega a aritmética imutável a RewardPoints.Add.
4. A aplicação salva usando a revisão carregada, impedindo que uma alteração concorrente seja sobrescrita silenciosamente.
5. O caso de uso retorna um resultado de aplicação, convertido pelo controller em um DTO de resposta.

Débito segue o mesmo fluxo de comando. Saldo e capacidade de pagamento são consultas: carregam um snapshot, mas nunca chamam TrySaveAsync. Falhas conhecidas chegam a ApiExceptionHandler, que as converte em HTTP Problem Details na fronteira externa.

## Arquitetura: Clean Architecture e Portas Hexagonais

~~~mermaid
flowchart LR
    Client([Cliente HTTP<br/>Swagger / Testes])

    subgraph Api["API · Adaptador Primário"]
        Controller["WalletsController<br/>Rotas · Validação · Status"]
        DTOs["DTOs de requisição e resposta<br/>Problem Details"]
        Composition["Program.cs<br/>Composition Root"]
        Controller --- DTOs
    end

    subgraph Application["Application · Casos de Uso"]
        InboundPorts["Portas de entrada<br/>Open · Credit · Debit<br/>Balance · Affordability"]
        UseCases["Implementações dos casos de uso"]
        InboundPorts --> UseCases
    end

    subgraph Domain["Domain · Núcleo de Negócio"]
        WalletPort["IRewardsWallet<br/>Credit · Debit · CanAfford"]
        Wallet["RewardsWallet<br/>Aggregate Root"]
        Points["RewardPoints + WalletName<br/>Value Objects imutáveis"]
        Rules["DomainAssertions<br/>DomainException"]
        RepositoryPort["IRewardsWalletRepository<br/>Porta de saída"]
        WalletPort --> Wallet
        Wallet -->|delega aritmética| Points
        Wallet -->|protege invariantes| Rules
        Points -->|valida valores| Rules
    end

    subgraph Infrastructure["Infrastructure · Adaptador Secundário"]
        Repository["PostgresRewardsWalletRepository<br/>Snapshots · Concorrência otimista"]
        DbContext["RewardFlowDbContext<br/>EF Core · Migrations"]
        Repository --> DbContext
    end

    Database[(PostgreSQL 17<br/>Volume Docker)]
    Client -->|HTTP| Controller
    Controller -->|chama porta| InboundPorts
    UseCases -->|invoca comportamento| WalletPort
    UseCases -->|carrega e salva| RepositoryPort
    Repository -.->|implementa| RepositoryPort
    DbContext -->|Npgsql| Database
    Composition -.->|conecta portas e adaptadores| InboundPorts
    Composition -.-> Repository
~~~

As setas contínuas mostram o fluxo das requisições e a colaboração no domínio. As tracejadas mostram a composição dos adaptadores: Program.cs escolhe as implementações e Infrastructure implementa a porta de repositório pertencente ao Domain.

Domain não possui referências a outros projetos ou pacotes. Application e Infrastructure referenciam Domain de forma independente. Infrastructure não conhece HTTP, Application não referencia Infrastructure e os objetos do domínio nunca são serializados diretamente como contratos HTTP.

## Estrutura do projeto

~~~text
RewardFlow.SuppleDesign/
├── RewardFlow.SuppleDesign.slnx
├── RewardFlow.SuppleDesign.slnLaunch              # Perfil de inicialização compartilhado opcional
├── Directory.Build.props                         # Configurações compartilhadas do .NET 8
├── compose.yaml                                  # PostgreSQL 17 e volume local
├── dotnet-tools.json                             # Versão reproduzível do dotnet-ef
├── README.md
├── README.pt-BR.md
│
├── src/
│   ├── RewardFlow.SuppleDesign.Domain/
│   │   ├── Entities/
│   │   │   ├── RewardsWallet.cs                  # Aggregate Root e ciclo de vida
│   │   │   └── RewardsWalletStatus.cs
│   │   ├── ValueObjects/
│   │   │   ├── RewardPoints.cs                   # Aritmética imutável
│   │   │   └── WalletName.cs                     # Nome normalizado
│   │   ├── Exceptions/DomainException.cs
│   │   ├── Ports/
│   │   │   ├── IRewardsWallet.cs                 # Contrato de comportamento
│   │   │   └── IRewardsWalletRepository.cs       # Porta de persistência
│   │   └── DomainAssertions.cs                   # Pré-condições e invariantes
│   ├── RewardFlow.SuppleDesign.Application/
│   │   └── Wallets/
│   │       ├── Common/
│   │       ├── OpenWallet/
│   │       ├── CreditPoints/
│   │       ├── DebitPoints/
│   │       ├── ChangeStatus/
│   │       ├── GetWallet/
│   │       ├── GetBalance/
│   │       └── CheckAffordability/
│   ├── RewardFlow.SuppleDesign.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/                   # Mapeamento Fluent do EF Core
│   │   │   ├── Migrations/                       # Esquema PostgreSQL versionado
│   │   │   ├── Models/                           # Registros exclusivos da persistência
│   │   │   ├── RewardFlowDbContext.cs
│   │   │   └── RewardFlowDbContextFactory.cs
│   │   ├── Repositories/PostgresRewardsWalletRepository.cs
│   │   └── DependencyInjection.cs
│   └── RewardFlow.SuppleDesign.Api/
│       ├── Endpoints/Wallets/WalletsController.cs
│       ├── DTOs/Wallets/
│       │   ├── WalletContracts.cs
│       │   └── WalletContractExamples.cs
│       ├── Errors/ApiExceptionHandler.cs
│       ├── Program.cs
│       └── RewardFlow.http
└── test/
    └── RewardFlow.SuppleDesign.Tests/
        ├── TestDoubles/InMemoryRewardsWalletRepository.cs
        ├── DomainTests.cs
        ├── ApplicationTests.cs
        ├── PostgresRepositoryTests.cs
        └── WalletApiTests.cs
~~~

## Blocos de construção de Evans e o RewardFlow

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

    subgraph RewardFlow["RewardFlow · Implementação concreta"]
        direction TB
        Wallet["RewardsWallet<br/>Entity + Aggregate Root"]
        Points["RewardPoints<br/>Value Object imutável"]
        Repository["IRewardsWalletRepository<br/>+ Adaptador PostgreSQL"]
        UseCases["Casos de uso da Application<br/>Open · Credit · Debit · Consultas · Ciclo de vida"]
        Construction["Construtores<br/>Criação simples; sem Factory"]
        Layers["Domain · Application<br/>Infrastructure · API"]
        Controllers["Controllers enxutos<br/>Somente aspectos HTTP"]
    end

    E --> Wallet
    A --> Wallet
    V --> Points
    R --> Repository
    S -.->|coordenação da aplicação| UseCases
    F -.->|a criação ainda é simples| Construction
    L --> Layers
    U -.->|alternativa evitada| Controllers

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
| Service | Casos de uso da Application | Coordenam fluxo e persistência; as regras permanecem no domínio. |
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

## Contrato HTTP e erros

| Método | Rota | Body | Sucesso |
| --- | --- | --- | --- |
| POST | /api/wallets | {"name":"Recompensas principais"} | 201, ID gerado e localização da carteira |
| GET | /api/wallets/{id} | nenhum | 200 e dados da carteira |
| POST | /api/wallets/{id}/credit | {"points":100} | 200 e saldo atualizado |
| POST | /api/wallets/{id}/debit | {"points":40} | 200 e saldo atualizado |
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

Uma carteira é inserida com revisão 0 e a chave primária protege a identidade gerada. Ao salvar, o adaptador executa uma atualização condicional por id e pela revision esperada, incrementando a revisão. Se outro comando já tiver salvo o mesmo snapshot, nenhuma linha será atualizada e a aplicação responderá 409.

A connection string é lida de ConnectionStrings:RewardFlow. Em outros ambientes, use ConnectionStrings__RewardFlow. A senha versionada serve apenas para desenvolvimento local.

### Modelo de dados (DER)

~~~mermaid
erDiagram
    REWARDS_WALLETS {
        uuid id PK "identidade do agregado"
        varchar name "nome normalizado"
        integer status "ativa, suspensa ou encerrada"
        timestamptz created_at_utc "instante de abertura"
        bigint points "saldo não negativo"
        bigint revision "concorrência otimista"
    }
~~~

WalletName e RewardPoints são armazenados de forma achatada porque pertencem à carteira. revision pertence à coordenação da persistência, não ao agregado. Constraints rejeitam nomes vazios, status desconhecidos, pontos negativos e revisões negativas.

### Migrations

As migrations estão versionadas em Infrastructure/Persistence/Migrations, incluindo a evolução incremental de perfil e ciclo de vida. Antes de ativar o middleware, Program.cs chama ApplyRewardFlowMigrationsAsync, que executa Database.MigrateAsync.

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

A suíte xUnit cobre:

- Igualdade por valor, aritmética imutável, limites, saldo insuficiente e overflow.
- Normalização do nome, identidade gerada, criação em UTC e transições do ciclo de vida.
- Resgate do saldo exato, proteção do saldo zero, identidade vazia e preservação do estado em falhas.
- Consultas de capacidade de pagamento sem mutação.
- Orquestração, snapshots desacoplados e rejeição de escritas obsoletas.
- Mapeamento PostgreSQL, migration no startup, persistência após reinício e atualização condicional por revisão.
- Rotas e DI reais pelo WebApplicationFactory, contratos, status HTTP e Problem Details.
- Exemplos claros no Swagger, independentes da implementação da validação HTTP.

Inicie o banco com docker compose up -d --wait antes de executar dotnet test RewardFlow.SuppleDesign.slnx. Os testes da aplicação usam o repositório em memória somente como test double rápido do mesmo contrato.

## Melhorias educacionais futuras

Cada evolução futura deve introduzir uma pressão concreta de modelagem e tornar visível seu efeito sobre o design. O objetivo é aprofundar o exemplo de Supple Design sem transformar o repositório em um catálogo de padrões de infraestrutura.

| Evolução para aprendizado | Pergunta de domínio a explorar | Foco em Supple Design |
| --- | --- | --- |
| Histórico de movimentações | Crédito e débito são apenas mudanças de saldo ou cada movimento possui identidade, motivo e valor para auditoria? | Descobrir novos Conceptual Contours e decidir se uma movimentação é uma Entity dentro da carteira ou pertence a outro Aggregate. |
| Expiração de pontos | Como pontos ganhos em momentos diferentes devem expirar sem quebrar as regras de resgate? | Criar conceitos temporais explícitos, Intention-Revealing Interfaces e Assertions, evitando comparações de datas espalhadas pelos casos de uso. |
| Política configurável de acúmulo | Como valor da compra, campanha e categoria do membro são convertidos em pontos? | Modelar o cálculo como uma Standalone Class com uma Side-Effect-Free Function, entradas explícitas e resultado previsível. |
| Transferência entre carteiras | Quais regras e qual fronteira de consistência governam uma movimentação entre duas carteiras? | Avaliar se o comportamento pertence a um Aggregate ou exige um Domain Service, a partir do modelo e não apenas do nome do padrão. |
| Comandos idempotentes | Como impedir que uma nova tentativa de crédito aplique duas vezes o mesmo evento de negócio? | Dar nomes claros à identidade do comando e ao tratamento de duplicidade, mantendo detalhes de transporte e persistência fora do modelo de recompensas. |
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
| [IRewardsWallet](src/RewardFlow.SuppleDesign.Domain/Ports/IRewardsWallet.cs) | Intention-Revealing Interfaces | [Capítulo 10](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [DomainAssertions](src/RewardFlow.SuppleDesign.Domain/DomainAssertions.cs) | Assertions e invariantes explícitas | [Capítulo 10](https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html) |
| [RewardsWallet](src/RewardFlow.SuppleDesign.Domain/Entities/RewardsWallet.cs) | Conceptual Contours, Entity e Aggregate Root | [DDD Reference](https://www.domainlanguage.com/ddd/reference/) |
| [IRewardsWalletRepository](src/RewardFlow.SuppleDesign.Domain/Ports/IRewardsWalletRepository.cs) | Repository como porta de saída do domínio | [DDD Reference](https://www.domainlanguage.com/ddd/reference/) |

## Como contribuir

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
