# Module contracts

> **Status:** Accepted — ARCH-01 (2026-08-25).

Shared database does **not** mean shared ownership.

A module must not:

- access another module’s `DbContext`
- query another module’s EF entity
- write another module’s tables

Communication is in-process: small exposure/read interfaces (current model: Room Operations ↔ Technical Service `IRoomServiceabilityLookup` / room directory). No event bus, no outbox, no async messaging until a real distributed need exists.

Naming: keep lookup/read interfaces next to the consuming application layer; implementations live in the providing module’s Infrastructure.

Do **not** create a broad BuildingBlocks project. Cross-cutting types allowed in API `Context` (actor, tenant) and existing module application contracts only. No Employee DTO dump, no shared HR errors in a kitchen-sink library.
