# Git Workflow

How I keep history readable for the next person, who is usually me. See [[Code Review Checklist]].

## Branches
Short-lived, named for the thing they do. A branch that lives two weeks turns its merge into an archaeology dig.

## Commits
- One logical change per commit.
- The message says *why*, not *what*. The diff already shows what.
- If I can't summarize it in a line, the commit is doing too much.

## Rebase vs Merge
I rebase my own feature branch to keep it tidy before review. I never rebase anything someone else has pulled. Merge to integrate, rebase to clean up, and don't mix the two up.

## Things That Save Me
- `git add -p` so I commit on purpose, not by accident.
- Small PRs. A 2000-line diff gets a rubber stamp, not a review.
- Pull before push. Boring, prevents tears.

## Rules I Won't Break
Never force-push shared branches. Never commit secrets. The second one is one `git filter-repo` headache too many.
