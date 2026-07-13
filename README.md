# Lottery Microservices System

![CI/CD](https://github.com/talya-git/project-angular-netCore/actions/workflows/ci.yml/badge.svg)

## One-Command Startup

```powershell
docker compose up --build
```

## Access Points

| Service | URL |
|---------|-----|
| Angular Client | http://localhost |
| Seq (Structured Logs) | http://localhost:8081 |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |
| BFF ping | http://localhost/api/bff/ping |

## Architecture

- **4 Microservices**: Auth, Gifts (×2 replicas), Inventory, Orders, Notification
- **Polyglot Persistence**: SQL Server (Auth/Inventory/Orders) + MongoDB (Gifts) + Redis (cache)
- **Async Saga**: RabbitMQ choreography — OrderPlaced → InventoryReserved/Rejected → Confirmed/Cancelled
- **API Gateway + Load Balancer**: nginx upstream across 2 Gifts replicas
- **BFF**: aggregates Orders + Gifts in one call
- **Observability**: Serilog → Seq, /health endpoints, Correlation ID

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for full architecture document and ADRs.
