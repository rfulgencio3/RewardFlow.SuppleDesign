# ADR-002: Runtime migrations for the educational API

- Status: Accepted
- Date: 2026-10-23

## Context

The sample should run locally with a short, repeatable sequence and show schema evolution through real EF Core migrations. Requiring a separate deployment tool would distract from the domain and adapter boundaries being taught.

## Decision

The API applies pending migrations before accepting HTTP requests. Startup prints a concise English status and tells the developer how to start PostgreSQL when it is unavailable. EF Core command logging is suppressed during normal execution.

## Consequences

- A fresh local database becomes ready when the API starts.
- Startup requires schema-changing database permissions.
- Production systems should review migrations and normally execute a migration bundle or reviewed SQL from the deployment process before starting application instances.
