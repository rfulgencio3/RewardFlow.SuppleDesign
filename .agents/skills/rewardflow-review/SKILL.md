---
name: rewardflow-review
description: Review RewardFlow code and documentation for business correctness and educational accuracy of Supple Design. Use for project assessments, change reviews and README, implementation and test alignment.
---

# Review RewardFlow

Read AGENTS.md, docs/constitution.md, docs/supple-design.md and relevant docs/guardrails.md criteria. docs/project-analysis.md is historical evidence; verify findings against current code.

Check English for documents, plans, ADRs, skills, diagram labels, code and comments. README.pt-BR.md is the explicit translation exception; align its technical claims with canonical README.md.

Trace operations from public contracts through Domain to persistence. Link pattern claims to methods and observable tests. Separate implemented behavior, tested behavior and future intent.

Check C# interfaces versus intention-revealing contracts; I/O versus calculations without mutation; type closure versus bounds/rejections; business contours versus technical folders; identity versus record equality.

For movements, examine replay, conflicting payloads, stale revisions and atomicity. A double does not prove PostgreSQL transactions. Check whether unrelated invalid fields mask HTTP validation tests. Review transfers against both revisions, paired ledger entries and cross-endpoint idempotency.

Report findings by impact with file/method, trigger, consequence and minimal correction. Recognize justified teaching choices. Do not introduce layers without an observed problem or silently change behavior during a wording review. Report checks and gaps without claiming absolute conformance.
