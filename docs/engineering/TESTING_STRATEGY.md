# Testing Strategy

This document defines HuGuWeb's testing philosophy. **Testing frameworks and tools are not yet selected.** The testing *direction* below is frozen as of the Sprint 0.3A Architecture Freeze.

---

## Purpose

Testing protects **business behavior**, not code coverage metrics.

HuGuWeb tests should give confidence that hotel operational workflows behave correctly, that changes do not introduce regressions, and that bug fixes stay fixed.

Do **not** optimize for meaningless coverage percentages.

---

## Bootstrap Testing Direction

Frozen as of Sprint 0.3A. Frameworks remain selectable at implementation.

| Category | When |
|----------|------|
| **Unit tests** | At project bootstrap |
| **Architecture tests** | At project bootstrap |
| **PostgreSQL integration tests** | When persistence is introduced |
| **API integration tests** | When API behavior exists |
| **End-to-end tests** | Only after real critical workflows exist |

Do not optimize for coverage percentage.

---

## Testing Layers

### Unit Testing

Test individual units of business logic in isolation.

- Focus on domain rules and business invariants
- Mock or stub external dependencies
- Fast execution for rapid feedback

### Integration Testing

Test interactions between components—e.g., application layer with persistence, or service with external integration boundary.

- Verify contracts between layers and modules
- Use realistic but controlled test environments
- Avoid testing third-party systems directly where mocks suffice

### Regression Testing

Verify that previously working behavior continues to work after changes.

- Bug fixes should include regression tests where practical
- Critical workflows should have regression coverage
- Regression tests protect against the [Change Safety Principle](ENGINEERING_PRINCIPLES.md#change-safety-principle)

### Architecture Tests

Where valuable, automated tests may enforce architectural boundaries—e.g., dependency direction, module isolation, or forbidden cross-module references.

Architecture tests are a tool, not a goal. Use them when they prevent real violations, not as boilerplate.

### Critical Workflow Testing

End-to-end or near-end-to-end tests for business-critical hotel workflows once defined and implemented.

These tests validate that connected operational behavior works as expected across module boundaries.

---

## Testing Principles

| Principle | Description |
|-----------|-------------|
| **Deterministic tests** | Tests must produce consistent results; avoid flaky tests |
| **Test isolation** | Tests must not depend on execution order or shared mutable state |
| **Regression on bug fixes** | When practical, every bug fix includes a test that would have caught the bug |
| **Protect business behavior** | Tests validate what the system should do, not implementation details |
| **Minimal regression surface** | Tests should be localized; a change in one area should not require rewriting unrelated tests |
| **No coverage theater** | High coverage with weak assertions provides false confidence |

---

## What Is Not Decided Yet

The following are intentionally deferred:

- Unit testing framework
- Integration testing framework
- End-to-end testing framework
- Test runner and CI integration
- Test data management strategy
- Performance/load testing approach

These will be selected when MVP development begins, subject to ADR approval. The *categories* and timing above are already frozen.

---

## Related Documents

- [Engineering Principles](ENGINEERING_PRINCIPLES.md)
- [Development Workflow](DEVELOPMENT_WORKFLOW.md)
