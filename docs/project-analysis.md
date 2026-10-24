# Análise do projeto em 24/10/2026

## Escopo e resultado

Foram examinados os quatro projetos de produção, portas, casos de uso, modelos e mapeamentos de persistência, startup, contratos HTTP, testes, READMEs, ADRs, configuração de build, Compose e CI. A execução `dotnet test RewardFlow.SuppleDesign.slnx --configuration Release --verbosity minimal` restaurou, compilou e concluiu **38 testes aprovados, zero falhas e zero ignorados**, com SDK 9.0.308 e PostgreSQL local saudável. Esta avaliação é um retrato dessa revisão, não um certificado de cobertura exaustiva.

O projeto atende bem à demonstração dos seis padrões centrais apresentados no guia. O principal espaço de melhoria está na precisão das explicações e na ligação entre afirmação, código e teste. Não há necessidade demonstrada de ampliar a arquitetura para alcançar o propósito atual.

## Modelo e fronteiras

`RewardPoints` concentra aritmética, igualdade por valor e limites. `RewardsWallet` controla saldo e ciclo de vida, com setters privados e validação anterior à atribuição. `WalletName` e `RewardReason` explicitam normalização e restrições. Datas entram explicitamente no domínio, enquanto a aplicação usa `TimeProvider`.

Casos de uso coordenam carga, comportamento e persistência sem regras de saldo no controller. Resultados de aplicação evitam devolver o agregado mutável ao HTTP. Domain não referencia projetos ou pacotes; Application referencia apenas Domain. Infrastructure depende de Domain; Api compõe Application e Infrastructure. Isso preserva o domínio sem EF Core ou ASP.NET.

O adapter PostgreSQL reconstitui instâncias isoladas, atualiza por revisão e salva movimentos em transação. O registro usa chave global de operação e ordenação por instante/ID. O histórico não é carregado com a carteira. Migrations no startup são uma decisão educacional registrada em ADR-002.

## Evidências fortes

| Evidência | O que sustenta |
| --- | --- |
| `ArithmeticReturnsNewValuesAndPreservesOperands` | Composição com novos valores e preservação dos operandos |
| `RejectedOperationsDoNotChangeBalance` | Rejeição de zero, saldo insuficiente e overflow sem alteração de saldo |
| `LifecycleStateGovernsWalletBehavior` | Crédito em suspensão, débito bloqueado e encerramento terminal |
| `RepeatedOperationIsIdempotentAndAppearsOnceInTheLedger` | Replay sequencial e conflito por alteração de dados |
| `StaleSnapshotCannotOverwritePersistedBalance` | Revisão obsoleta não sobrescreve estado no adaptador real |
| `DuplicateTransactionRollsBackBalanceUpdate` | Falha de inserção do extrato reverte a alteração do saldo |
| `ConcurrentIdenticalCreditsConvergeOnOneMovement` | Duas chamadas reais de crédito convergem no cenário exercitado |
| `CreditRetryIsIdempotentAndLedgerIsPaged` | Contrato HTTP de replay e páginas com `hasNextPage` |

Os testes estão em [test/RewardFlow.SuppleDesign.Tests](../test/RewardFlow.SuppleDesign.Tests).

## Achados e prioridades

### Precisão didática — prioridade alta

O README associa interfaces C# a Intention-Revealing Interfaces. A escolha é válida como recurso local, mas o leitor precisa entender que nomes e contratos de classes concretas também expressam intenção. Do mesmo modo, camadas técnicas não são a definição de Conceptual Contours.

`ListRewardTransactions` tem comentário de Side-Effect-Free Function, mas realiza I/O. Pode ser descrito como consulta sem mutação; os melhores exemplos de cálculo previsível estão em `RewardPoints`. `CanAfford` não muda estado, mas depende do estado atual da carteira. O [guia](supple-design.md) explicita essas diferenças.

O capítulo também discute design declarativo. O projeto demonstra seis padrões e não oferece uma lição dedicada sobre esse tema. A documentação deve declarar esse limite. Não é preciso implementar uma DSL para preencher uma lista.

### Precisão do README e exemplos — prioridade alta

O README inglês afirma que comandos e consultas de saldo retornam apenas `points`; créditos e débitos retornam `RewardMovementResponse`, com `balanceAfter`. Também diz que consultas de capacidade em carteiras suspensas são rejeitadas; o código retorna false. Essas afirmações precisam corresponder ao comportamento.

O exemplo `.http` usa IDs de operação fixos e globais. Executá-lo com outra carteira sem trocar os IDs pode produzir 409. O material de contribuição deve orientar novos IDs para operações novas e preservação do ID em retries.

### Testes — prioridade média

`InvalidPointsReturnBadRequest` usa corpos sem `operationId` e `reason`. O status 400 pode decorrer desses campos, mascarando regressões na validação de pontos. Em futura alteração desse teste, mantenha os demais campos válidos para isolar o comportamento.

O README inglês atribui à suíte cenários como persistência entre reinícios da API e ausência de consulta COUNT. A inspeção não encontrou testes específicos que provem essas duas afirmações. Persistência é compatível com o adapter e volume; isso é distinto de teste de reinício. O repository usa `pageSize + 1`, mas não há teste instrumentando SQL para impedir COUNT.

Faltam exemplos dedicados para limites de motivo, instantes não UTC, enum inválido, replay de débito e reutilização de ID em outra carteira/direção. A suíte atual não deve ser apresentada como cobertura completa dessas combinações. Acrescente testes quando corrigir ou evoluir os comportamentos correspondentes.

### Contratos e concorrência — prioridade média

`ListRewardTransactions.ExecuteAsync` depende dos limites definidos no controller; chamadas diretas podem fornecer página/tamanho inválidos e causar overflow ou paginação sem sentido. Antes de um segundo adapter, convém tornar a pré-condição explícita na aplicação e testá-la.

O double em memória bloqueia `TrySaveMovementAsync`, mas `TrySaveAsync` e leituras não usam o mesmo lock. Portanto, ele não demonstra isolamento completo diante de movimentos e transições simultâneos. Testes concorrentes críticos devem continuar no PostgreSQL.

O teste de créditos concorrentes usa `Task.WhenAll` sem barreira que force ambos a carregar a mesma revisão. É uma evidência útil do cenário executado, mas não garante que toda janela de corrida tenha sido exercitada.

### Identidade e invariantes entre objetos — prioridade baixa

`RewardTransaction` é descrita como Entity, mas um record compara todos os seus campos. O `operationId` identifica o negócio e é chave no banco; a semântica de `Equals` merece explicação antes de usar transações como entidades em coleções.

A porta aceita carteira e transação separadas. O adapter não verifica que `WalletId` e `BalanceAfter` do registro correspondem à carteira recebida. Os casos de uso atuais constroem o par corretamente; não afirmar que a assinatura protege, sozinha, essa consistência contra qualquer chamador.

## Decisão para esta entrega

Criar instruções centrais, guia, guardrails e skills locais; alinhar READMEs e registrar os achados. Manter os achados de implementação como próximos incrementos explícitos, sem aproveitar a documentação para mudar contratos ou regras. Uma evolução futura deve partir de um exemplo concreto e mostrar o efeito no modelo.
