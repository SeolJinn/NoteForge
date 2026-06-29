# Code Review Checklist

The list I run through silently before approving a PR. Companion to [[Clean Architecture]] and [[Debugging Strategies]].

## Read the Description First

If the PR description doesn't tell me what changed and why, the review is already in trouble. Ask for context before reading code.

## On a First Pass

- Does the change do what the description says? Anything extra?
- Are there tests? Do they exercise the change rather than the framework?
- Is anything obviously dangerous — concurrency, file paths, SQL, secrets?

## On the Logic

- Are the inputs validated where they enter the system? Are they NOT re-validated five layers deep?
- Are errors handled or propagated? Are they logged with enough context to debug?
- Is there a comment explaining a *why* that the code can't?

## On Style

- Names. The single highest-leverage thing in code review. Push back on bad ones.
- Function length. Over 30 lines is a smell, not a sin.
- Premature abstraction. Three uses earn an abstraction; two don't.

## On What's Missing

- Documentation, especially public API surface.
- Migration steps if the change isn't backwards compatible.
- Telemetry — can we see if this thing is broken in production?

## How to Phrase Comments

- Mark blocking comments clearly. Optional ones too.
- "Why?" is more useful than "I don't like this."
- Suggest, don't dictate. The author owns the code.

## When to Approve

When you'd be comfortable owning the change yourself. Not when nothing is wrong — when nothing is *importantly* wrong.
