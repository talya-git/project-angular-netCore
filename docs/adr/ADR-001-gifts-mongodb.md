# ADR-001: GiftsService — MongoDB (Document Store)

## Status
Accepted

## Context
GiftsService manages gift catalog data. Each gift can have varying attributes depending on its category (e.g., a car gift has different fields than a vacation package). A rigid relational schema forces NULL columns or extra join tables for every new attribute type.

## Decision
Use **MongoDB** (document database) for GiftsService.

## Rationale
- **Schema flexibility**: each gift document can carry its own set of attributes without schema migrations.
- **BASE model is acceptable**: catalog reads are eventually consistent — a slight delay in seeing a new gift is tolerable.
- **CAP**: MongoDB favors CP (Consistency + Partition tolerance). In a catalog context, partition tolerance is more important than 100% availability.
- **No cross-document transactions needed**: gifts and donors are written independently; no ACID multi-entity transactions required.

## Alternatives Considered
- SQL Server: strong ACID, but requires schema migration for every new gift attribute — too rigid for a catalog.

## Consequences
- No EF Core; use MongoDB.Driver directly.
- `Id` fields are ObjectId strings instead of integers.
