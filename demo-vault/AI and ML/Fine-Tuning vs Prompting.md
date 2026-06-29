# Fine-Tuning vs Prompting

When to reach for weights and when to just write better instructions. Related to [[Retrieval Augmented Generation]].

## Start With Prompting
Always. It's free, instant, and you'd be amazed how far a few good examples in the context window get you. Most "we need to fine-tune" requests are actually "we wrote a lazy prompt."

## When Prompting Runs Out
- You need a consistent output format the model keeps drifting away from.
- The task has a style or domain vocabulary that won't fit in a prompt.
- You're paying for thousands of tokens of instructions on every call and want to bake them in.

## The Middle Ground
LoRA changed the math. Instead of updating all the weights you train a small adapter, often under 1% of parameters. Cheap enough to run on one GPU, good enough for most adaptation.

## What I Tell People
Reach for retrieval before fine-tuning when the problem is missing knowledge, not missing behavior. Fine-tuning teaches a skill. It doesn't teach facts.
