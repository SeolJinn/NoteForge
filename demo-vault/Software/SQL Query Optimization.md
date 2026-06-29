# SQL Query Optimization

Where I go when the app is slow and the code looks fine. Pairs with [[Database Indexing]] and [[Caching Strategies]].

## First Move, Always
Run `EXPLAIN ANALYZE`. Stop guessing. The planner will tell me it's doing a sequential scan over two million rows, and then the problem is obvious.

## The Usual Suspects
- The N+1 query. One query becomes a thousand inside a loop.
- `SELECT *` dragging back columns nobody uses.
- A function wrapped around an indexed column, quietly killing the index.

## What Usually Fixes It
- The right index, confirmed by the query plan, not by hope.
- Fetching in one query instead of in a loop.
- Filtering and paginating in the database, not in app memory.

## Measuring Honestly
- Test against production-sized data. Everything is fast on ten rows.
- Watch rows examined, not just time. Time lies when the cache is warm.

## My One Rule
Make it correct, then make it measured, then make it fast. In that order, every time.
