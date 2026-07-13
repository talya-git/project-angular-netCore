# ADR-002: OrdersService — SQL Server (Relational)

## Status
Accepted

## Context
OrdersService handles financial transactions: placing orders, tracking amounts, and managing order items. These operations involve multiple entities (Order + OrderItems) that must succeed or fail together.

## Decision
Use **SQL Server** (relational database) for OrdersService.

## Rationale
- **ACID is mandatory**: an order and its items must be written atomically. Partial writes (order created, items missing) are unacceptable in a financial context.
- **Strong consistency**: the CAP theorem trade-off here favors Consistency over Availability — we must never show an incorrect order total.
- **Relational integrity**: foreign keys between Orders and OrderItems enforce data correctness at the database level.
- **BASE is not acceptable**: eventual consistency in financial data leads to real money errors.

## Alternatives Considered
- MongoDB: flexible schema but BASE consistency model — not suitable where money is involved.

## Consequences
- Uses EF Core with SQL Server provider.
- Supports full ACID transactions across Order + OrderItems in a single SaveChanges call.
