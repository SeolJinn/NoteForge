# Dependency Injection

Just passing things in instead of reaching out for them. Related to [[Clean Architecture]] and [[Testing Pyramid]].

## The Whole Idea
A class declares what it needs in its constructor. Something else decides what to hand it. That's it. The fancy frameworks are optional.

## Why I Bother
- Tests can pass in a fake and stop hitting the real database.
- Wiring lives in one place instead of scattered `new` calls.
- Swapping an implementation doesn't mean a hunt through the codebase.

## Constructor Injection, Almost Always
Dependencies go in the constructor so an object is fully formed the moment it exists. Property injection leaves half-built objects lying around, and I always forget which half.

## Where People Overdo It
- A container for a 200-line script. Just call `new`.
- Six layers of interfaces with exactly one implementation each.
- Service-locator pulling from a global. That's DI cosplay, not DI.

## The Quiet Benefit
The constructor becomes an honest list of what a class actually depends on. If that list is huge, the class is doing too much.
