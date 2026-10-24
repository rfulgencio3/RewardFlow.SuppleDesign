---
name: rewardflow-evolve
description: Evoluir regras, value objects e casos de uso do RewardFlow preservando sua demonstração educacional de Supple Design. Use para alterações de comportamento do modelo; não para correções isoladas de texto.
---

# Evoluir o modelo do RewardFlow

Leia `AGENTS.md` na raiz e `docs/guardrails.md`. Use `docs/supple-design.md` para distinguir o padrão de sua adaptação C#.

Antes de editar, formule a regra com entrada, resultado e caso rejeitado. Identifique quem possui o estado e a invariante: pontos calculam, carteira governa saldo/ciclo de vida, aplicação coordena e adapter persiste. Leia o caso de uso afetado e seus testes.

Implemente a menor mudança que torne o conceito explícito. Para novas abstrações, explique a necessidade concreta e o custo para quem está aprendendo. Não acrescente frameworks ou padrões apenas para aumentar a demonstração.

Teste o comportamento na camada que decide a regra. Mudança em movimento exige verificar estado preservado em falha; se tocar persistência, valide revisão, idempotência e atomicidade com PostgreSQL. Use relógio explícito para exemplos temporais.

Atualize comentários e os dois READMEs apenas nas afirmações afetadas. Registre ADR quando alterar uma fronteira ou decisão com efeito em vários tipos. Ao concluir, informe exemplo antes/depois, benefício educacional, testes executados e limites restantes.
