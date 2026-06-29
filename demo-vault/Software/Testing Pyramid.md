# Testing Pyramid

A shape that reminds me where to spend my testing budget. Companion to [[Code Review Checklist]].

## The Shape
Lots of unit tests at the bottom. Fewer integration tests in the middle. A handful of end-to-end tests at the top. Wide and fast below, narrow and slow above.

## Why The Shape Matters
- Unit tests are cheap, fast, and pinpoint failures.
- E2E tests are slow, flaky, and tell you *something* broke but not where.
- Invert the pyramid and your suite takes an hour and lies to you.

## Where I Actually Put Effort
- Unit test the logic that's easy to get wrong: parsing, money, dates, edge cases.
- Integration test the seams, especially anything touching a database.
- A few E2E tests on the critical user paths and nothing more.

## The Ice Cream Cone Anti-Pattern
All E2E, no units. Looks thorough, runs forever, fails randomly, and nobody trusts it. Seen it sink a team's velocity.

## What I Believe
A test that's flaky is worse than no test. It trains everyone to ignore red.
