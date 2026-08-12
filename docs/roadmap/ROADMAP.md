# Roadmap

> **Important:** This roadmap documents high-level phases only. It does **not** include dates, feature commitments, or MVP scope.

---

## Phase 0 — Product Discovery & Foundation

**Current phase.**

Establish product and engineering foundation:

- Product vision, principles, and glossary
- Engineering principles and development workflow
- Testing strategy philosophy
- Architecture documentation and ADR system
- Competitor and market research context
- Roadmap and future scope documentation

No application code, technology selection, or MVP implementation during this phase.

---

## Phase 1 — Product Definition

Define product scope based on research and evidence:

- Validate hotel operational problems with target users
- Define MVP scope with explicit Product Owner approval
- Prioritize research areas into MVP, Next, Future, Integrate, or Reject
- Document user workflows (workflow-oriented, not module-oriented)
- Establish success criteria for pilot validation

---

## Phase 2 — Architecture & Technology Decisions

Make and document approved architecture and technology decisions:

- Evaluate Modular Monolith and alternatives via ADR
- Select backend, frontend, database, and infrastructure stack
- Define module boundaries and integration architecture
- Evaluate multi-property requirements for architectural impact
- Select testing frameworks and CI/CD approach
- Define security and observability approach

All significant decisions recorded as ADRs.

---

## Phase 3 — MVP Development

Implement approved MVP scope:

- Follow sprint lifecycle and development workflow
- Maintain change safety and module isolation principles
- Automated testing per testing strategy
- CTO code review and Product Owner acceptance before commits

Specific MVP features are not defined in this document.

---

## Phase 4 — Pilot Hotel Validation

Validate MVP with real hotel operations:

- Deploy to pilot hotel environment
- Collect operational feedback
- Measure against success criteria defined in Phase 1
- Identify gaps, bugs, and prioritization for expansion

---

## Phase 5 — Expansion

Expand product based on validated learnings:

- Prioritize Next and Future scope items
- Evaluate integration partnerships
- Consider multi-property and hotel chain requirements
- Evaluate employee mobile application (see [Future Scope](../product/FUTURE_SCOPE.md))
- Consider industry expansion only after hospitality MVP is validated

Specific expansion features are not defined in this document.

---

## Related Documents

- [Product Vision](../product/PRODUCT_VISION.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [Future Scope](../product/FUTURE_SCOPE.md)
- [Engineering Principles](../engineering/ENGINEERING_PRINCIPLES.md)
- [Development Workflow](../engineering/DEVELOPMENT_WORKFLOW.md)
