# Logging and Observability

The difference between fixing a 3am incident in ten minutes or three hours. Pairs with [[Debugging Strategies]] and [[Error Handling Patterns]].

## Logging vs Observability
Logging is what I write down. Observability is whether I can answer questions I didn't think to ask in advance. The second is the actual goal.

## What I Log
- **Structured, not strings.** Key-value pairs I can query, not prose I have to grep.
- A correlation ID on every request so I can follow one journey across services.
- Enough context to act, never a password or token.

## Log Levels, Used Honestly
- `ERROR` means someone should wake up.
- `WARN` means something's off but we survived.
- `INFO` is the story of normal operation.
- `DEBUG` is for me, and it's off in production.

## The Three Pillars
Logs tell me what happened. Metrics tell me how often and how bad. Traces tell me where the time went. I want all three before an incident, not after.

## Hard-Won Rule
If I only find out a thing broke because a user told me, my observability failed. Alert on symptoms users feel, not on every twitchy metric.
