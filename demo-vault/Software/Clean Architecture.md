# Clean Architecture

The shape that keeps surviving regime changes. Companion to [[Code Review Checklist]] and [[API Design Notes]].

## The Core Idea

Push the things that change for business reasons inward. Push the things that change for technology reasons outward. Domain logic should not know that a database, a web framework, or a cloud provider exists.

## Layers, Inside Out

1. **Entities.** Pure business rules and types. Zero framework dependencies.
2. **Use cases.** Application-specific orchestration. Receives plain data, returns plain data.
3. **Interface adapters.** Translate between use cases and the outside world. Repositories, controllers, presenters.
4. **Frameworks and drivers.** The web framework, the ORM, the cloud SDK.

## The Dependency Rule

Source code dependencies point inward only. The inner layers know nothing about the outer ones. When the use case needs to save something, it calls an interface; the implementation lives outside.

## What This Buys You

- The domain is testable without spinning up a database.
- The web framework is replaceable. So is the database.
- New developers can read the use cases and understand the system without learning the stack.

## What It Costs

- More files. Sometimes a lot more.
- More indirection. Sometimes pointless indirection if the project is small.
- A learning curve for people who came up writing controllers that talk to ORMs directly.

## When Not to Use It

CRUD apps with no real business logic. Internal tools where nobody will replace the framework. Throwaway prototypes. Don't impose architecture where there's nothing to architect.
