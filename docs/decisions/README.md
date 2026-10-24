# Architecture decisions

These short records explain choices that are important to the educational model but are not obvious from a single class.

| Decision | Purpose |
| --- | --- |
| [ADR-001](001-transaction-ledger-boundary.md) | Keep the wallet small while recording identified, idempotent movements. |
| [ADR-002](002-runtime-migrations.md) | Explain why this educational API applies migrations during startup. |
| [ADR-003 — Proposed](003-atomic-wallet-transfers.md) | Plan atomic 1..100-point transfers, paired ledger entries and global operation identity. Not implemented. |

An accepted decision can be replaced when new domain evidence appears. The replacement should preserve the history and link back to the superseded record.
