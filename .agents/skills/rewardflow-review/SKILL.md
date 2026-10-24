---
name: rewardflow-review
description: Revisar código e documentação do RewardFlow quanto a correção das regras e precisão didática de Supple Design. Use para avaliação do projeto, revisão de mudanças ou alinhamento entre README, implementação e testes.
---

# Revisar o RewardFlow

Leia `AGENTS.md`, `docs/supple-design.md` e os critérios relevantes de `docs/guardrails.md`. `docs/project-analysis.md` é um retrato anterior: confirme os achados no código atual.

Siga uma operação do contrato público até o domínio e a persistência. Relacione cada afirmação de padrão ao método que a demonstra e ao teste que observa seu efeito. Separe comportamento implementado, comportamento testado e intenção futura.

Verifique especialmente: interface C# versus intenção do contrato; consulta com I/O versus cálculo sem mutação; fechamento das operações versus limites e rejeições; contorno de negócio versus pasta técnica; identidade operacional versus igualdade de records.

Quando a mudança afetar movimentos, examine replay, dados conflitantes, revisão obsoleta e gravação de saldo/extrato. Não aceite um double como evidência suficiente de transações PostgreSQL. Nos testes HTTP de validação, verifique se outros campos inválidos podem mascarar a regra examinada.

Reporte achados por impacto, com arquivo/método, situação concreta, consequência e correção mínima sugerida. Reconheça escolhas didáticas justificadas e não proponha novas camadas sem problema observável. Em revisão solicitada, não altere comportamento apenas para corrigir a redação. Informe verificações executadas e lacunas sem declarar conformidade absoluta.
