# Database Indexing

The cheapest performance win, right up until it isn't. Pairs with [[SQL Query Optimization]] and [[Caching Strategies]].

## What An Index Buys
A sorted lookup structure so the database stops scanning every row. Reads get fast. That's the whole pitch.

## What It Costs
- Every write now maintains the index too.
- Disk space, sometimes a lot.
- A planner that can pick the wrong one and make things slower.

## Composite Indexes
Order matters. An index on `(tenant_id, created_at)` helps queries that filter by tenant first. It does almost nothing for a query that only filters by `created_at`. Left-to-right or it's dead weight.

## How I Decide
- Look at the actual slow queries, not the imagined ones.
- Index the columns in `WHERE`, `JOIN`, and `ORDER BY`.
- Run `EXPLAIN` and confirm the index is actually used before celebrating.

## Mistakes I Keep Making
Adding indexes hopefully instead of from evidence. Five unused indexes slow down every insert and help nothing.
