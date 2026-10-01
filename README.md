# Multi-Tenant Order & Inventory Platform

B2B SaaS for distributors: products, multi-warehouse stock, purchase and sales orders.
Modular .NET monolith + Angular, deployed to Azure.

## Layout
- `src/backend/` .NET solution
- `src/frontend/` Angular app
- `infra/` Bicep templates
- `docs/` diagrams and decisions

## Local development
```bash
docker compose up -d
```
| Service | Port |
|---|---|
| SQL Server | 1433 |
| RabbitMQ UI | 15672 |
| Redis | 6379 |
| Azurite | 10000-10002 |
| Mailpit UI | 8025 |
