# Memory Leaks and Profiling

The slow-motion bug. Fine for an hour, dead by morning. Companion to [[Debugging Strategies]].

## How A Leak Feels
- Memory creeps up and never comes back down.
- Performance degrades the longer the process runs.
- A restart "fixes" it, which is the tell, not the cure.

## Where They Hide
- Event handlers and subscriptions nobody unsubscribed.
- A cache with no eviction that quietly becomes the whole heap.
- Static collections that only ever grow.

## How I Find Them
- Take a heap snapshot, run the workload, take another, diff them.
- Look at what's growing, then ask who's holding the reference.
- Reproduce in a loop so the leak adds up fast enough to see.

## Profiling Discipline
- Measure before changing anything. My intuition about hotspots is usually wrong.
- Profile under realistic load, not a toy benchmark.
- Optimize the thing the profiler points at, not the thing I assumed.

## What I Tell Myself
A leak isn't usually exotic. It's almost always something I forgot to let go of.
