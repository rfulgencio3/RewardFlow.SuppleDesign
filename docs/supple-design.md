# Supple Design no RewardFlow

Este guia interpreta os padrões no contexto do código existente. O capítulo 10 é a referência; os exemplos C#, as camadas e os guardrails são decisões locais. O [sumário oficial do livro](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217) identifica os seis padrões abaixo e também discute design declarativo. O [DDD Reference de Eric Evans](https://www.domainlanguage.com/ddd/reference/) é material complementar ao livro.

## Caminho de estudo

Leia `RewardPoints`, execute seus testes, avance para `RewardsWallet` e só depois siga o caso de uso de crédito até PostgreSQL e HTTP. Cada exemplo deve permitir responder: qual dificuldade do negócio esta escolha torna mais fácil de compreender e modificar?

| Padrão | Evidência neste projeto | Como observar | Limite da demonstração |
| --- | --- | --- | --- |
| Intention-Revealing Interfaces | `Credit`, `Debit`, `CanAfford`, `Suspend` e `RewardReason` usam vocabulário explícito | Ler uma chamada sem inspecionar sua implementação; seguir `OpenCreditDebitAndReadCoordinateTheAggregate` | Interface é o contrato público de uso; não exige criar um tipo `interface` para cada classe |
| Side-Effect-Free Functions | `RewardPoints.Add` e `Subtract` retornam novos valores; `CanAfford` não muda a carteira | `ArithmeticReturnsNewValuesAndPreservesOperands` e `CanAffordIsPureAndIncludesTheExactBalance` | `CanAfford` depende do estado atual do receptor; casos de uso que leem banco dependem de I/O e do instante da leitura |
| Assertions | Construtores válidos, saldo suficiente, overflow e transições explícitas | `ArithmeticRejectsOverflowAndNegativeResults`, `RejectedOperationsDoNotChangeBalance`, `LifecycleStateGovernsWalletBehavior` | Não se resume a uma classe de validação ou a `Debug.Assert`; regras permanecem ativas em Release |
| Conceptual Contours | Pontos calculam; carteira governa saldo e ciclo de vida; transação registra a movimentação aceita | Ler os três tipos e ADR-001; seguir `DuplicateTransactionRollsBackBalanceUpdate` | Pastas e camadas, sozinhas, não demonstram contornos do modelo |
| Standalone Classes | Value objects têm significado próprio e podem ser usados sem carteira, HTTP ou banco | Construir e calcular `RewardPoints` diretamente nos testes de domínio | Os tipos ainda colaboram com assertions de domínio; standalone não significa ausência absoluta de dependências |
| Closure of Operations | `Add(RewardPoints)` e `Subtract(RewardPoints)` retornam `RewardPoints` | Compor `new RewardPoints(100).Add(new RewardPoints(25)).Subtract(new RewardPoints(40))`, obtendo 85 | A subtração é parcial nos não negativos e a soma tem limite de `long`; entradas inválidas são rejeitadas |

Os arquivos estão em [Domain](../src/RewardFlow.SuppleDesign.Domain) e os exemplos executáveis em [DomainTests.cs](../test/RewardFlow.SuppleDesign.Tests/DomainTests.cs) e [ApplicationTests.cs](../test/RewardFlow.SuppleDesign.Tests/ApplicationTests.cs).

## Um exemplo de ponta a ponta

Uma carteira ativa abre com zero pontos. Um crédito de 100 cria saldo 100; um débito de 40 resulta em 60. Consultar `CanAfford(60)` retorna true sem salvar; debitar 61 é rejeitado e preserva o saldo. Suspender a carteira mantém créditos disponíveis, bloqueia débitos e faz `CanAfford` retornar false, inclusive para zero pontos.

O caso de uso recebe os dados, constrói valores válidos e invoca o agregado. A persistência salva saldo e transação juntos usando a revisão carregada. Repetir a operação aceita com o mesmo `operationId`, carteira, direção, pontos e motivo normalizado devolve o registro original. Esse replay pode ocorrer depois de a carteira mudar de estado: não representa uma nova movimentação.

## O que a arquitetura ensina

Value Object, Entity, Aggregate e Repository são blocos de DDD que dão suporte aos exemplos. Clean Architecture, Portas e Adaptadores, EF Core e idempotência ajudam a executar e proteger o modelo; sua presença não comprova, por si só, Supple Design.

`IRewardsWallet` tem uma única implementação e é mantida como contrato didático explícito. Nomes claros nas classes concretas também demonstram intenção. `RewardTransaction` tem identidade operacional, mas é um `record` C# com igualdade estrutural: identidade de negócio e semântica de igualdade da linguagem devem ser explicadas separadamente.

Design declarativo não tem demonstração dedicada neste projeto. Não apresentar os seis exemplos como cobertura integral do capítulo, nem introduzir uma DSL ou Specifications sem uma pergunta concreta do domínio.

## Como acrescentar uma lição

Descreva uma regra com exemplo válido, caso rejeitado e efeito esperado no estado. Escolha o conceito que deve possuir essa regra. Escreva um teste de comportamento e implemente a menor mudança que torne a regra visível. Explique qual padrão ajuda o leitor e o que ele não garante. Use os [guardrails](guardrails.md) para revisar a mudança.
