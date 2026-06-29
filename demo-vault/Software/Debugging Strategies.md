# Debugging Strategies

Notes from too many late nights. Pairs with [[Code Review Checklist]].

## Reproduce First

The single most valuable thing in debugging is a reliable repro. Spend the first 30 minutes shrinking the failure case until it fits on one screen. If you can't repro, you can't fix — you can only guess.

## Bisect

When did it last work? `git bisect` is uncannily effective for "this used to work" bugs. It's also a useful frame even without git: between the last good state and the current bad state, what changed?

## Don't Trust the Stack Trace

The line where the exception is thrown is rarely the line where the bug lives. Read the stack like a chain of *callers* and ask which one passed bad data.

## Print Statements Are Underrated

A debugger is great for stepping through one execution. Logs are better for understanding the shape of many executions. When in doubt, log everything — you can delete it later.

## Rubber-Duck

Explain the bug out loud, in complete sentences, as if to someone unfamiliar with the code. Half the time you find the bug before you finish.

## Categories of Bug

- **Off-by-one.** Always plausible.
- **Race conditions.** Usually disguised as "intermittent."
- **State that should have been reset.** Globals and singletons.
- **Time and time zones.** Particularly daylight saving boundaries.
- **Encoding.** UTF-8 vs UTF-16 vs ASCII fallback chains.
- **Numeric precision.** Float comparisons, integer overflow.

## When to Stop and Sleep

If you've been at it for three hours and you're more confused than when you started, stop. Sleep on it. Come back fresh. The number of bugs I've solved in the shower is embarrassing.
