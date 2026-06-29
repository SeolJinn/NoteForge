# REST vs gRPC

Not a religious war, just a tradeoff. Adjacent to [[API Design Notes]].

## Where REST Wins
- Public APIs. Anyone with curl can poke at it.
- Browsers talk to it natively, no toolchain.
- Easy to cache, easy to debug, easy to explain.

## Where gRPC Wins
- Service-to-service inside my own network.
- Strict contracts from a `.proto` file, codegen on both sides.
- Streaming and binary framing, much less overhead per call.

## The Honest Differences
REST is text over HTTP that humans can read. gRPC is a compact binary protocol you need tools to inspect. One is friendly, one is fast.

## How I Choose
- Crossing an org boundary or hitting a browser? REST.
- Tight internal mesh with latency budgets? gRPC.
- Unsure? Start with REST. It's harder to regret.

## The Catch
gRPC through proxies, load balancers, and browsers is fiddly. Half my gRPC time has gone to infrastructure, not code.
