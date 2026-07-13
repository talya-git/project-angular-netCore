# ADR-003: GiftsService — Redis (Key-Value Cache)

## Status
Accepted

## Context
GiftsService catalog reads are frequent and the data changes rarely. Every HTTP request hitting MongoDB adds latency. A caching layer can dramatically reduce read latency and MongoDB load.

## Decision
Use **Redis** as a distributed cache for GiftsService reads (cache-aside pattern).

## Rationale
- **Key-value family**: Redis is a key-value store — perfect for caching by a simple key (e.g., `gifts:all`).
- **BASE model is acceptable for cache**: a cache hit may return slightly stale data; this is tolerable for a gift catalog.
- **CAP**: Redis in single-node mode favors CP. Cache misses fall back to MongoDB automatically.
- **Invalidation strategy**: when a gift is created or updated, the cache key is deleted (write-through invalidation), forcing the next read to repopulate from MongoDB.
- **Polyglot**: this gives us a second NoSQL family (key-value) alongside MongoDB (document), satisfying the polyglot persistence requirement.

## Alternatives Considered
- In-memory cache (IMemoryCache): not distributed — each service replica has its own cache, causing inconsistency under load balancing.

## Consequences
- Adds Redis container to docker-compose.
- Cache hit/miss visible in logs.
- TTL set to 5 minutes as a safety net even without explicit invalidation.
