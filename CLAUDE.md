# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Current state

The design document `Multi-Tenant Order & Inventory Platform Architecture, Stack, Cost and Interview Guide (1).docx` at the repo root is the source of truth for the intended design. Re-read it (e.g. `unzip -p *.docx word/document.xml`) before making architectural decisions.

Built so far: repo scaffold and docker-compose, the .NET solution with Module 1 (Tenant & Identity: tenant registration, login, refresh-token rotation, tenant isolation, permission checks), an Angular skeleton (login, register, dashboard, users list, auth interceptors/guards) and a CI workflow. Modules 2-9, Azure/Bicep and the deployment pipeline are not built yet. Azure is only needed at deployment time.

## Commands

Local dependencies (SQL Server, RabbitMQ, Redis, Azurite, Mailpit; Docker Desktop must be running): `docker compose up -d`

Backend (run in `src/backend`, .NET 10, `InventoryPlatform.slnx`):
- Build / all tests: `dotnet build`, `dotnet test`. Integration tests start a SQL Server container through Testcontainers, so Docker must be running.
- One test: `dotnet test --filter "FullyQualifiedName~Tenant_cannot_read_another_tenants_users"`
- Run the API on http://localhost:5141: `dotnet run --project src/InventoryPlatform.Api --launch-profile http`
- Add a migration: `dotnet ef migrations add <Name> -p src/InventoryPlatform.Infrastructure -s src/InventoryPlatform.Api -o Persistence/Migrations` (`dotnet-ef` is a local tool: `dotnet tool restore`)
- Apply migrations to the local database: `dotnet ef database update -p src/InventoryPlatform.Infrastructure -s src/InventoryPlatform.Api`. The API does not migrate at startup. Tests migrate their own container.

Frontend (run in `src/frontend`, Angular 22, zoneless, Vitest):
- `npm start` serves http://localhost:4200 and proxies `/api` to http://localhost:5141 (`proxy.conf.json`), so no CORS setup is needed.
- `npx ng build`, `npx ng test --no-watch`. One spec file: `npx ng test --no-watch --include src/app/core/auth/jwt.spec.ts`
- No linter is configured yet.

CI (`.github/workflows/ci.yml`) runs the backend and frontend jobs only for the paths that changed. `ci-gate` is the single check to require on `main`.

Quirks: MediatR is pinned to 12.5.0 because newer versions need a paid license. Dev-only secrets (a JWT key and the local SQL password) live in `src/backend/src/InventoryPlatform.Api/appsettings.Development.json`. The Angular CLI sometimes adds an analytics ID to `angular.json`; do not commit that.

Login is by company code (tenant slug) + email + password, since users are tenant-scoped.

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
