# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Current state

The repository contains **no code yet**, only the design document `Multi-Tenant Order & Inventory Platform Architecture, Stack, Cost and Interview Guide (1).docx`. It is not a git repository. That document is the source of truth for the intended design. Re-read it (e.g. `unzip -p *.docx word/document.xml`) before making architectural decisions. Update this file with real build/test commands once the scaffolding exists. The commands below are the planned ones, not verified.

## Project

A multi-tenant B2B SaaS platform for distributors and wholesalers (products, multi-warehouse stock, purchase orders, sales orders). It is a **modular .NET monolith + Angular frontend on Azure**, designed to run for about ₹0-100 using free tiers and local Docker. It doubles as an interview project for a .NET full-stack developer, so every pattern should be explainable "three levels deep". Prefer fewer, fully understood features.

Nine modules, built in this order: Tenant & Identity, Product Catalog, Inventory, Purchase Orders, Sales Orders, Payments, Notifications, Reporting, Audit. The minimum core is modules 1, 3, 5, 7 plus the outbox pattern, Service Bus, Key Vault, Container Apps and CI/CD.

## Planned layout (monorepo)

- `src/backend/`: .NET 8+ solution (API, Application, Domain, Infrastructure, extracted services, tests)
- `src/frontend/`: Angular 17+ app (standalone components, lazy feature routes)
- `infra/`: Bicep templates plus dev/prod parameter files
- `.github/workflows/`: Actions pipelines (backend, frontend, infra)
- `docs/`: diagrams, decisions, runbook
- `docker-compose.yml`: local SQL Server, RabbitMQ, Redis, Azurite, Mailpit

## Architecture (needs several files to understand)

- **Modular monolith first**. Notifications and Payments are later extracted as separate containers. Modules are separated by folders, interfaces and one SQL schema each (`identity`, `catalog`, `inventory`, `purchasing`, `sales`, `messaging`, `audit`).
- **Clean Architecture** dependency rule: Domain depends on nothing; Application → Domain; Infrastructure → Application/Domain; API → Application/Infrastructure. Infrastructure implements interfaces that Application defines.
- **CQRS with MediatR**: commands use EF Core and domain rules; queries/reports use Dapper (stored procedures such as `sp_MonthlySalesSummary`, `sp_StockValuation`, view `vw_LowStockProducts`). MediatR pipeline behaviors handle validation, logging, transaction and audit.
- **Multi-tenancy**: shared database with a `TenantId` column, resolved from the JWT, enforced by an EF Core global query filter. **Dapper and raw SQL bypass that filter, so every such query must filter on `TenantId` explicitly.** An integration test must prove tenant A cannot read tenant B's data. Every index leads with `TenantId`.
- **Auth**: ASP.NET Core Identity + JWT with refresh tokens. Permission-based authorization (e.g. `Orders.Approve`, stored as rows mapped to per-tenant roles), not just roles. Angular guards and `*hasPermission` are UX only; the API must enforce permissions again.
- **Messaging reliability**: outbox (event saved to `messaging.OutboxMessages` in the same transaction as the business change, a background worker publishes to Service Bus), idempotent consumers (`InboxMessages` records processed IDs), and an `Idempotency-Key` header on payment and order APIs. MassTransit abstracts RabbitMQ (local) vs Service Bus (Azure).
- **Concurrency**: `rowversion` on `StockLevels` and `SalesOrders`. `DbUpdateConcurrencyException` leads to retry or HTTP 409.
- **Inventory**: `StockMovements` is an append-only ledger and the source of truth; `StockLevels` is a cached summary that can be rebuilt.
- **Order state machine**: Placed → Reserved → Paid → Shipped → Delivered, and Cancelled only before Shipped (releases the reservation). The domain validates every transition.
- **Other conventions**: money is `decimal(18,2)`; status columns are tinyint mapped to C# enums with check constraints; soft delete via `IsDeleted` + global filter; audit via EF Core interceptors writing JSON old/new values; RFC 7807 ProblemDetails from one exception middleware; correlation ID flows through logs and messages; Polly for retries and circuit breakers on external calls; API versioning `/api/v1/...`; health endpoints `/health/live` and `/health/ready`.
- **Frontend**: feature folders under `features/` (catalog, inventory, purchasing, sales, reports, admin) plus `core/` (auth, tenant, notifications, interceptors, guards) and `shared/`. Auth interceptor queues retries so only one refresh call runs on 401. Use Signals-based services for feature state and NgRx only for cross-feature state (current user/permissions). Use OnPush, `track`/trackBy, and `debounceTime` + `switchMap` for search. Static Web Apps needs a fallback route to `index.html` in `staticwebapp.config.json`.

## Database migrations

EF Core code-first. In the pipeline, generate a migration bundle or idempotent SQL script and run it as a **separate deployment step**, never at app startup (instances would race). Migrations must be backward compatible.

## Git, CI/CD and environments

- Trunk-based: short-lived branches named `feature/<area>-<what>`, `fix/`, `infra/`, `chore/`, `docs/`, `hotfix/`. Squash-merge via PR into a protected `main`. Use conventional commits, e.g. `feat(orders): add outbox table and publisher`.
- PR checks: build, unit + integration tests (Testcontainers SQL Server), Angular lint/tests, Bicep validation, with path filters for frontend-only or backend-only changes.
- Merge to `main` builds images tagged with the commit SHA, pushes to GitHub Container Registry, and deploys to dev (`rg-orders-dev`). Prod (`rg-orders-prod`) deploys the **same image** (never rebuild) after a manual approval or `v*` tag, with gradual Container Apps traffic shifting. Rollback means moving traffic to the previous revision.
- Azure auth from Actions via OpenID Connect; app secrets in Key Vault with Managed Identity; no secrets in Bicep or the repo.

## Cost constraints (affects design choices)

Develop locally with Docker Compose, using RabbitMQ and local Redis in place of Service Bus and Azure Redis. Use GHCR instead of Azure Container Registry. Use APIM Consumption or YARP, never APIM Developer/Basic. Use Service Bus Standard only for short demo windows and delete paid resources after demos. Set a Cost Management budget alert first.
