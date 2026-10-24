# Guardrails de implementação e revisão

Estes critérios orientam colaboradores e agentes. Testes e CI verificam somente os comportamentos que exercitam; não verificam automaticamente qualidade didática ou fidelidade conceitual.

## Propósito e escopo

Uma alteração deve manter o modelo fácil de explicar com exemplos de fidelidade. Novas funcionalidades precisam de uma regra concreta e de benefício educacional identificável. Expiração, campanhas, transferências e autenticação são possibilidades futuras, não requisitos implícitos. Ajustes de infraestrutura podem ser necessários, mas devem permanecer proporcionais ao exemplo.

## Critérios verificáveis

| Guardrail | Onde revisar | Evidência exigida quando afetado |
| --- | --- | --- |
| Pontos inteiros entre zero e `long.MaxValue`; movimentos estritamente positivos | `RewardPoints`, `RewardsWallet` | Testes de limites, operação rejeitada preservando estado e resgate do saldo exato |
| Nomes e motivos normalizados e válidos; identidade não vazia; instantes UTC | Value objects, construtores e casos de uso | Exemplos válidos e inválidos diretamente no núcleo |
| Regras de ciclo de vida pertencem à carteira | `RewardsWallet` | Estado de origem, destino e operações permitidas; encerramento é terminal |
| Consultas não salvam nem criam carteiras | Casos de uso de consulta | Resultado e persistência inalterada; não chamar uma consulta com I/O de função pura |
| Domain não depende de outros projetos; Application depende de Domain; núcleo sem pacotes externos | `.csproj` e imports | Revisão do diff; build não impede que alguém acrescente uma dependência indevida |
| HTTP valida formato e traduz erros; domínio protege negócio independentemente do HTTP | DTOs, controller e handler | Teste HTTP com os demais campos válidos e teste direto da regra |
| Alteração do saldo e registro de movimento são atômicos | Repository e ADR-001 | Teste com PostgreSQL demonstrando rollback diante de conflito |
| Escrita exige revisão; snapshots são isolados | Porta e ambos os adaptadores | Teste de escrita obsoleta e de mutação local que não vaza para o armazenamento |
| `operationId` é global; replay retorna resultado original; dados diferentes conflitam | Crédito, débito e repository | Repetição, conflito e concorrência nos caminhos alterados |
| Extrato permanece separado da reconstituição da carteira | Repository e ADR-001 | Consulta paginada limitada, ordenação determinística e carteira carregada sem histórico |

Não use `TrySaveAsync` para novos fluxos de crédito/débito da aplicação: movimentos usam `TrySaveMovementAsync`. Não transforme o test double em prova de atomicidade do PostgreSQL. O double atual também não tem a mesma sincronização entre todos os métodos; testes de concorrência relevantes devem usar o adaptador real.

## Abstrações e explicações

Antes de adicionar tipo, interface, serviço ou framework, descreva a regra ou fronteira que ele representa e por que os tipos atuais não bastam. Reutilização hipotética e nomes de padrões não justificam camadas. Remova a abstração se ela esconder mais vocabulário do que esclarece. Esta regra pode ser reconsiderada diante de uma necessidade real, com motivação registrada.

Explique intenção e consequência nos comentários educacionais. Não marque todo método com um padrão sem mostrar o benefício. Assertions abrangem pré/pós-condições e invariantes; os testes ajudam a demonstrar a consequência. Standalone Classes admite colaborações pequenas. Closure of Operations descreve o tipo do resultado das operações aceitas, sem prometer álgebra irrestrita.

## Verificação proporcional

Para mudança de código, execute na raiz:

```powershell
docker compose up -d --wait
dotnet test RewardFlow.SuppleDesign.slnx --configuration Release
```

Para investigar o núcleo sem PostgreSQL, use `dotnet test RewardFlow.SuppleDesign.slnx --configuration Release --filter "FullyQualifiedName~DomainTests|FullyQualifiedName~ApplicationTests"`. Esse recorte não substitui persistência e HTTP antes de concluir uma mudança que os afete.

Para documentação, confira caminhos, contratos, exemplos e coerência entre READMEs. Skills novas devem passar por `quick_validate.py` da skill-creator, quando disponível. Não crie testes que só procuram frases em Markdown.

A CI atual executa restore, build Release e a suíte com PostgreSQL. Não há check específico de dependências arquiteturais, seleção de skills ou qualidade pedagógica. Registre checks não executados e sua causa, sem apresentar revisão textual como garantia automatizada.

## Orientação para pessoas e ferramentas

Leia [AGENTS.md](../AGENTS.md) e use a skill apropriada à tarefa. Ferramentas compatíveis com descoberta local podem carregar as skills em `.agents/skills`; outras precisam receber os arquivos como contexto. Pessoas usam os mesmos critérios na revisão. Invocar uma skill não concede permissão para publicar, enviar mensagens ou alterar recursos externos.
