# Architecture Document — Lottery Microservices System

## 1. System Overview

This system evolved from a monolithic .NET 8 API into a distributed, production-style microservices architecture.
It supports browsing gifts, placing orders, reserving inventory, and notifying the customer of the final order state.

---

## 2. Final Architecture Diagram

```
                        ┌─────────────────────────────────────────────────────┐
                        │                   Docker Network                    │
                        │                                                     │
  Browser               │   ┌──────────────────────────────────────────────┐  │
    │                   │   │              nginx (API Gateway)             │  │
    └──── HTTP:80 ──────┼──▶│  /api/Auth   → auth-service                 │  │
                        │   │  /api/Gifts  → upstream (gifts-1, gifts-2)  │  │
                        │   │  /api/Donor  → upstream (gifts-1, gifts-2)  │  │
                        │   │  /api/Inventory → inventory-service         │  │
                        │   │  /api/Orders → orders-service               │  │
                        │   │  /api/bff    → bff-service                  │  │
                        │   └──────────────────────────────────────────────┘  │
                        │         │           │          │         │           │
                        │         ▼           ▼          ▼         ▼           │
                        │   ┌──────────┐ ┌────────┐ ┌───────┐ ┌────────┐     │
                        │   │  auth-   │ │gifts-1 │ │invent-│ │orders- │     │
                        │   │ service  │ │gifts-2 │ │service│ │service │     │
                        │   └────┬─────┘ └───┬────┘ └───┬───┘ └───┬────┘     │
                        │        │           │          │         │           │
                        │        ▼           ▼          ▼         ▼           │
                        │   ┌────────┐ ┌─────────┐ ┌───────┐ ┌────────┐     │
                        │   │AuthDb  │ │ MongoDB │ │Invent-│ │OrdersDb│     │
                        │   │(SQL)   │ │+ Redis  │ │Db(SQL)│ │ (SQL)  │     │
                        │   └────────┘ └─────────┘ └───────┘ └────────┘     │
                        │                                                     │
                        │   ┌─────────────────────────────────────────────┐  │
                        │   │              RabbitMQ (Message Broker)      │  │
                        │   │  order.placed → inventory.reserved/rejected │  │
                        │   └─────────────────────────────────────────────┘  │
                        │                                                     │
                        │   ┌──────────┐                                      │
                        │   │   BFF    │ ← aggregates Orders + Gifts          │
                        │   └──────────┘                                      │
                        └─────────────────────────────────────────────────────┘
```

---

## 3. Services

| Service | Port | Database | Role |
|---------|------|----------|------|
| auth-service | 5001 | SQL Server (AuthDb) | Registration, Login, JWT |
| gifts-service (×2) | 5002, 5005 | MongoDB + Redis | Gift catalog, load balanced |
| inventory-service | 5003 | SQL Server (InventoryDb) | Stock management, Saga consumer |
| orders-service | 5004 | SQL Server (OrdersDb) | Order placement, Saga orchestration |
| bff-service | 5006 | — | Aggregates Orders + Gifts |
| nginx | 80 | — | API Gateway + Load Balancer |
| rabbitmq | 5672 | — | Async messaging broker |
| redis | 6379 | — | Distributed cache |
| mongodb | 27017 | — | Document store for gifts |
| sqlserver | 1433 | — | Relational store for Auth/Inventory/Orders |

---

## 4. Architecture Decision Records (ADRs)

### ADR-001: GiftsService → MongoDB

**Context:** The gift catalog contains items with varying attributes per category.
A car gift has different fields than a vacation package.

**Decision:** Use MongoDB (document database).

**Rationale:**
- Schema flexibility — each gift document carries its own attribute set without migrations.
- BASE consistency model is acceptable for a catalog — slight staleness is tolerable.
- CAP: MongoDB favors CP (Consistency + Partition tolerance), suitable for catalog reads.
- No cross-document ACID transactions needed.

**Alternative rejected:** SQL Server — too rigid, requires schema migration for every new gift attribute type.

---

### ADR-002: OrdersService → SQL Server (Relational)

**Context:** Orders involve financial transactions across multiple entities (Order + OrderItems).

**Decision:** Use SQL Server with EF Core.

**Rationale:**
- ACID is mandatory — an order and its items must be written atomically.
- Strong consistency (CP in CAP) — we must never show an incorrect order total.
- Relational integrity enforced at the database level via foreign keys.
- BASE is not acceptable when money is involved.

**Alternative rejected:** MongoDB — BASE consistency model is unsuitable for financial data.

---

### ADR-003: GiftsService → Redis (Key-Value Cache)

**Context:** Catalog reads are frequent; gift data changes rarely.

**Decision:** Use Redis as a distributed cache with the cache-aside pattern.

**Rationale:**
- Key-value family — perfect for caching by a simple key (`gifts:all`).
- BASE model acceptable for cache — a stale cache hit is tolerable for a gift catalog.
- Distributed — consistent across both gifts-service replicas (unlike IMemoryCache).
- Invalidation strategy: cache key deleted on every write (write-through invalidation) + 5-minute TTL as safety net.
- Polyglot bonus: adds a second NoSQL family (key-value) alongside MongoDB (document).

**Alternative rejected:** IMemoryCache — not distributed, causes inconsistency under load balancing.

---

### ADR-004: RabbitMQ → Async Messaging

**Context:** The order flow originally used synchronous HTTP calls from OrdersService to InventoryService.
This creates tight coupling and fails if InventoryService is temporarily down.

**Decision:** Use RabbitMQ with choreography-based Saga.

**Rationale:**
- Decouples services — OrdersService does not need to know InventoryService's address.
- At-least-once delivery — messages survive broker restarts (durable queues).
- Choreography (vs. orchestration) — each service reacts to events independently, no central coordinator needed.
- Idempotency: consumers check order status before updating to prevent duplicate processing.

**Alternative considered:** Kafka — better for high-throughput event streaming and replay, but adds operational complexity (ZooKeeper/KRaft). RabbitMQ is simpler for request/reply saga patterns with low message volume.

---

## 5. Order Saga Flow (Choreography)

### Happy Path
```
Client → POST /api/Orders
  OrdersService: saves Order{Status=Pending}, publishes OrderPlaced{orderId, correlationId, items}
  InventoryService: receives OrderPlaced, checks stock → reduces stock, publishes InventoryReserved
  OrdersService: receives InventoryReserved → Order.Status = "Confirmed"
```

### Compensation Path (out of stock)
```
Client → POST /api/Orders (item with stock=0)
  OrdersService: saves Order{Status=Pending}, publishes OrderPlaced
  InventoryService: receives OrderPlaced, stock insufficient → publishes InventoryRejected{reason}
  OrdersService: receives InventoryRejected → Order.Status = "Cancelled"  ← compensation
```

---

## 6. Correlation ID Tracing

Every HTTP request carries an `X-Correlation-Id` header.
Every RabbitMQ message carries the same `CorrelationId` in the message properties.

This allows tracing a single order's complete journey across all services:

```
X-Correlation-Id: 3fa85f64-5717-4562-b3fc-2c963f66afa6

[OrdersService]    Order 42 created. CorrelationId=3fa85f64...
[RabbitMQ]         Publishing OrderPlaced. CorrelationId=3fa85f64...
[InventoryService] Processing OrderPlaced. CorrelationId=3fa85f64...
[InventoryService] Inventory RESERVED. CorrelationId=3fa85f64...
[OrdersService]    Order 42 CONFIRMED. CorrelationId=3fa85f64...
```

---

## 7. Phase Completion Summary

| Phase | Component | Status |
|-------|-----------|--------|
| Phase 1 | Monolith + Docker Compose | ✅ |
| Phase 2 | 4 Microservices + database-per-service | ✅ |
| Phase 2 | Polyglot persistence (MongoDB + Redis + SQL Server) | ✅ |
| Phase 2 | ADR documents | ✅ |
| Phase 3 | API Gateway (nginx) | ✅ |
| Phase 3 | BFF (aggregates Orders + Gifts) | ✅ |
| Phase 3 | Load balancing (2× gifts-service replicas) | ✅ |
| Phase 4 | RabbitMQ async messaging | ✅ |
| Phase 4 | Order Saga (choreography) | ✅ |
| Phase 4 | Compensation path (out-of-stock) | ✅ |
| Phase 4 | Redis cache-aside + invalidation | ✅ |
| Phase 5 | /health endpoints (all services) | ✅ |
| Phase 5 | Correlation ID across services + broker | ✅ |

---

## 8. One-Command Startup

```powershell
git clone https://github.com/talya-git/project-angular-netCore
cd project-angular-netCore
docker compose up --build
```

Access points:
- Angular client: http://localhost
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- BFF: http://localhost/api/bff/ping
