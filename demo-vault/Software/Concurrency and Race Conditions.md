# Concurrency and Race Conditions

The bugs that only show up in production, never on my laptop. See [[Debugging Strategies]] when one of these finally bites.

## How They Announce Themselves
- "Works fine locally, fails one time in fifty under load."
- A test that's green 99 runs and red on the 100th.
- Logs that make no sense because two threads interleaved mid-write.

## What Actually Causes Them
- Shared mutable state with no lock, or a lock held over the wrong scope.
- Check-then-act gaps. The world changes between the `if` and the `do`.
- Assuming a method is atomic because it's one line in my language.

## Deadlocks
Two locks, two threads, opposite order. Everyone waits forever. The fix is boring and reliable: always acquire locks in the same global order, and don't hold a lock while calling into code you don't control.

## How I Hunt Them
- Stop guessing. Add a stress test that hammers the path from many threads.
- Make it reproduce before I try to fix it. A fix I can't verify is a coin flip.
- Reach for immutability or a queue before reaching for another mutex.

## What I've Learned
Most race conditions are a design smell, not a missing lock. If correctness depends on timing, the design is asking for trouble.
