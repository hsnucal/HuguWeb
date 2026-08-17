# Development Workflow

This document describes the HuGuWeb development process, roles, and sprint lifecycle.

---

## Roles and Responsibilities

### Product Owner

- Owns product priorities
- Validates business requirements
- Challenges technical decisions when appropriate
- Performs product acceptance

The Product Owner does **not** automatically dictate implementation details.

### CTO / Lead Developer

- Challenges product requirements when necessary
- Owns technical direction
- Performs architecture review
- Performs code review
- Evaluates technical impact and risk
- Protects maintainability

The CTO does **not** automatically dictate product scope.

### Cursor / AI Coding Agent

Role: **implementation assistant**

- Follows approved prompts and sprint requirements
- Does **not** make final product decisions
- Does **not** make final architecture decisions
- Does **not** select technologies
- Does **not** independently expand scope
- Does **not** commit or push without explicit authorization

AI agents implement what is approved; they do not define what should be built.

---

## Collaboration Model

For every major feature, the Product Owner and CTO jointly evaluate:

- Real hotel problem
- Business value
- Usage frequency
- Purchasing decision impact
- Differentiation potential
- MVP necessity
- Implementation complexity
- Maintenance cost
- Architectural impact
- Security/compliance risk
- Build vs Integrate

Both sides are expected to challenge assumptions. Decisions should be based on evidence and explicit reasoning.

---

## Sprint Lifecycle

Each sprint follows this lifecycle:

| Step | Activity |
|------|----------|
| 1 | **Sprint Goal** — Define what the sprint achieves |
| 2 | **Requirements** — Document approved requirements |
| 3 | **Architecture Impact Analysis** — Assess technical and architectural impact |
| 4 | **Cursor Implementation Prompt** — Authorize implementation with explicit scope |
| 5 | **Implementation** — Execute approved work |
| 6 | **Automated Tests** — Add or update tests per [Testing Strategy](TESTING_STRATEGY.md) |
| 7 | **CTO Code Review** — Review code quality, architecture alignment, and risk |
| 8 | **Change / Impact Analysis** — Assess impact on unrelated areas |
| 9 | **Product Owner Testing** — Validate against business requirements |
| 10 | **Bug Fixes** — Address issues found during review and testing |
| 11 | **Regression Testing** — Verify no unintended side effects |
| 12 | **Product Owner + CTO Approval** — Explicit approval before commit |
| 13 | **Commit** — Commit only after approval |
| 14 | **Sprint Close** — Document outcomes and open items |

---

## Commit Authorization

> **Implementation completion does NOT automatically authorize a commit.**

A sprint's implementation phase may produce working changes in the working tree, but commits require explicit Product Owner and CTO approval after review, testing, and impact analysis.

During the foundation stage, documentation changes are left in the working tree for review before commit.

---

## Architecture Decisions

Significant architecture decisions must be recorded as Architecture Decision Records (ADRs). See [Architecture](../architecture/README.md) and [ADR Template](../architecture/adr/ADR-TEMPLATE.md).

Sprint 0.3A Architecture Freeze accepted the ADRs listed in [Architecture](../architecture/README.md). Remaining open decisions (module boundaries, cloud provider, CI/CD, and similar) are recorded in [Technology Decisions](../architecture/TECHNOLOGY_DECISIONS.md). Implementation is still not authorized in this phase.

---

## Forbidden Without Authorization

Implementation agents and developers must not independently:

- Select technology stack components
- Expand product scope beyond approved requirements
- Create application code during documentation-only sprints
- Commit or push without approval
- Introduce infrastructure (Docker, CI/CD, databases) without ADR approval

---

## Related Documents

- [Engineering Principles](ENGINEERING_PRINCIPLES.md)
- [Testing Strategy](TESTING_STRATEGY.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [Roadmap](../roadmap/ROADMAP.md)
