# Docker Basics

"Works on my machine" finally became true for everyone else too. Loosely related to [[Logging and Observability]].

## The Mental Model
An image is a frozen filesystem plus a command. A container is a running instance of one. I stop conflating the two and most confusion goes away.

## Dockerfile Habits
- Order layers from least to most likely to change. Dependencies before source.
- Pin versions. `latest` is a future surprise I'm scheduling for myself.
- One process per container. It's not a tiny virtual machine.

## Keeping Images Small
- Multi-stage builds so the final image ships runtime, not the whole toolchain.
- A slim base image. The difference between 1.2GB and 80MB is real.
- A `.dockerignore` so I don't copy `node_modules` and `.git` into the build.

## What Trips Me Up
- Data vanishing because I forgot it lives in a volume, not the container.
- Networking. It's always networking.

## Honest Take
Docker solved environment drift and handed me orchestration as the next problem.
