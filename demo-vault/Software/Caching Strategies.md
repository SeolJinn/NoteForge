# Caching Strategies

A cache is just a bet that the past predicts the future. Related: [[Database Indexing]] and [[SQL Query Optimization]].

## The Two Hard Parts
Naming things and cache invalidation. The second one is why every cache eventually becomes a source of bugs.

## Patterns I Reach For
- **Cache-aside.** Read miss, fetch, populate. Simple, and the app stays in control.
- **Write-through.** Write hits cache and store together. Fewer stale reads, slower writes.
- **TTL on everything.** Even if I think the data never changes. It does.

## Invalidation
The honest options are: expire by time, or evict on write. I prefer TTLs because they fail safe. Event-based invalidation is correct in theory and forgotten in practice the moment someone adds a new write path.

## What Bites Me
- Caching the empty result and never refreshing it.
- A stampede when a hot key expires and a thousand requests rebuild it at once.
- Storing user-specific data under a shared key. Found out the fun way.

## Rule of Thumb
Don't cache until I've measured. A cache hides the real problem and adds a new one.
