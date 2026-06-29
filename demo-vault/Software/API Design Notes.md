# API Design Notes

A grab-bag of opinions formed by writing and consuming a lot of APIs. References [[Clean Architecture]] and [[Embeddings and Vector Search]].

## Resources Over Verbs

`POST /users/123/promote` is worse than `POST /users/123/role { "value": "admin" }`. Custom verbs proliferate; resource-shaped APIs compose.

## Pagination

Always paginate. Always. The collection that's small today will be big in production. Cursor-based beats offset-based for anything mutable — offsets shift when items are inserted or deleted between calls.

## Errors

A consumable error has three things: a stable machine-readable code, a human-readable message, and enough context to act. `400 Bad Request` with `{"error": "validation"}` is hostile. `400` with `{"code": "field_too_long", "field": "name", "max": 200}` is a gift.

## Versioning

Version in the URL is ugly and works. Version in a header is elegant and gets ignored. Pick the one your consumers will actually obey.

## Idempotency

Any operation a client might retry needs an idempotency key. Payments, sends, anything that costs money or sends a message — assume the client will retry, and design accordingly.

## Defaults Matter

Defaults are the API. 90% of consumers will use them. If your default is "scan the entire collection", that's the API for 90% of consumers. Pick defaults that are safe in production.

## Documentation

The example beats the spec. The spec beats the prose. The prose beats nothing. If the docs don't have a working example I can paste into a terminal, the API is functionally undocumented.
