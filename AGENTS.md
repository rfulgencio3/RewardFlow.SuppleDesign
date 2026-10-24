# RewardFlow: instruções para alterações

O propósito é ensinar os padrões de Supple Design do capítulo 10 de DDD, de Eric Evans, com exemplos objetivos e executáveis em C#. Clareza do modelo é o critério principal; Clean Architecture e HTTP/PostgreSQL sustentam a demonstração.

## Antes de alterar

- Leia o trecho relevante do README, [guia dos padrões](docs/supple-design.md) e [guardrails](docs/guardrails.md). Consulte os ADRs quando a mudança tocar suas decisões.
- Para evolução de regras, value objects ou casos de uso, use [.agents/skills/rewardflow-evolve/SKILL.md](.agents/skills/rewardflow-evolve/SKILL.md).
- Para revisão de design ou precisão educacional, use [.agents/skills/rewardflow-review/SKILL.md](.agents/skills/rewardflow-review/SKILL.md).
- Correções pequenas de texto não exigem carregar ambas as skills. Se a ferramenta não descobrir skills, leia o arquivo indicado diretamente.

## Regras essenciais

- Preserve invariantes no domínio, aritmética imutável em RewardPoints e consultas sem mutação. HTTP e EF Core ficam nos adaptadores; Domain e Application permanecem sem pacotes de terceiros.
- Preserve snapshots isolados, revisão otimista, idempotência global por operationId e atomicidade entre saldo e extrato. Extrato não é reconstituído dentro da carteira (ADR-001).
- Justifique novas abstrações por uma regra ou fronteira concreta. Não introduza mediator, repositório genérico, event bus ou serviços apenas para ilustrar nomes de padrões.
- Intention-Revealing Interfaces não exige interface C#. Consulta de banco sem escrita não equivale a função pura. Closure of Operations não torna subtração total nem soma ilimitada.
- Explique o benefício de cada exemplo. Comentários educacionais são intencionais, mas não devem atribuir a Evans escolhas específicas deste projeto.
- Atualize os dois READMEs quando alterar afirmações compartilhadas. Registre limites e evidências; não declare cobertura que os testes não demonstram.

## Verificação

Para código ou configuração de execução: `docker compose up -d --wait` e `dotnet test RewardFlow.SuppleDesign.slnx --configuration Release`. PostgreSQL é necessário para testes de persistência e HTTP. Para alterações restritas a docs, confira links, exemplos e diff; não é necessário repetir a suíte sem mudança de comportamento.

Informe o que mudou, a motivação educacional, as verificações executadas e limitações. Não trate estas instruções como verificação automática: os guardrails distinguem revisão humana de checks executáveis.
