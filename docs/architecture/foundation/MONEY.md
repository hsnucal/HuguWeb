# Money

> **Status:** Accepted — ARCH-01 (2026-08-25). **Freeze only** — no Money library.

- Monetary amounts are `decimal` only. Never `float` / `double`.
- Currency is explicit (ISO 4217 code) when an amount is stored.
- Rounding belongs to the owning business process (not a shared helper unless a real use appears).
- Do not wrap every current BES field in a Money type now. Architecture tests forbid floating-point money on core domain types.
