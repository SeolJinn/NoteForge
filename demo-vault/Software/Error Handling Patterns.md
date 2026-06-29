# Error Handling Patterns

How code behaves when the world misbehaves, which is always. See [[Logging and Observability]] and [[API Design Notes]].

## My Default Stance
Fail loud, fail early, fail with context. A swallowed exception is a bug I get to discover much later, in production, with no clues.

## Exceptions vs Results
- Exceptions for the genuinely exceptional. The disk is gone, the network died.
- Return values for the expected. "User not found" is not exceptional, it's Tuesday.
- Using exceptions for normal control flow makes the happy path impossible to read.

## What A Good Error Carries
- What was being attempted.
- What was expected versus what happened.
- Enough to act on, without leaking internals to whoever's on the other side.

## Things I Refuse To Do
- `catch (Exception) {}`. The empty catch block. A crime scene with no body.
- Catching everything just to log and rethrow the exact same thing.
- Wrapping an error so many times the original cause disappears.

## The Principle
Handle errors where I can actually do something about them. Everywhere else, let them rise with their context intact.
