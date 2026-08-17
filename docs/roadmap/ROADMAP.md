# Roadmap

> **Important:** This roadmap documents high-level phases only. It does **not** include dates, feature commitments, or MVP scope.

---

## Phase 0 — Product Discovery & Foundation

**Current phase.**

### Completed / in progress foundation

- Product vision, principles, and glossary
- Engineering principles and development workflow
- Testing strategy philosophy
- Architecture documentation and ADR system (Sprint 0.3A Architecture Freeze — ADRs **Accepted**; implementation not authorized)
- Roadmap and future scope documentation

### Sprint 0.2 additions (Product Definition & Research Consolidation)

- Competitor research consolidation → [Competitor Analysis](../research/COMPETITOR_ANALYSIS.md)
- Initial discovery / pilot segment recorded (Independent Mid-Size; assumptions unvalidated) → [Target Customer](../product/TARGET_CUSTOMER.md)
- Hotel department problem discovery → [Hotel Problems](../product/HOTEL_PROBLEMS.md)
- Opportunity prioritization framework → [Opportunity Matrix](../product/OPPORTUNITY_MATRIX.md)
- MVP candidate grouping (not MVP freeze) → [MVP Candidates](../product/MVP_CANDIDATES.md)
- Build vs Integrate evaluation → [Build vs Integrate](../product/BUILD_VS_INTEGRATE.md)
- Pilot interview question guide → [Discovery Questions](../product/DISCOVERY_QUESTIONS.md)
- Evidence model for scope decisions → [Evidence Model](../product/EVIDENCE_MODEL.md)
- Market research ERP/PMS distinction → [Market Research](../research/MARKET_RESEARCH.md)

### Sprint 0.3A additions (Architecture & Technology Decision Analysis and Freeze)

- ADRs **Accepted** by Product Owner and CTO on 2026-08-17 → [ADR index](../architecture/adr/README.md)
- Technology decision summary → [Technology Decisions](../architecture/TECHNOLOGY_DECISIONS.md)

Sprint 0.3A freezes the architecture and technology baseline listed in those ADRs. It does **not** freeze MVP scope, define final business modules, select a cloud provider, or authorize application code.

Remaining open decisions include: final business module boundaries, multi-tenancy implementation, mobile technology, cloud provider, caching technology, message broker, background job library, external OIDC vendor, observability vendor, and CI/CD platform.

### Phase 0 constraints (unchanged)

No application code, scaffolding, dependency installation, or MVP implementation during this phase.

Accepted ADRs are architecture approval for the listed baseline. They do not authorize scaffolding or application code in Phase 0.

---

## Phase 1 — Product Definition

Define product scope based on research and evidence:

### Discovery execution

- Conduct pilot/customer interviews using [Discovery Questions](../product/DISCOVERY_QUESTIONS.md)
- Progress findings through [Evidence Model](../product/EVIDENCE_MODEL.md) (target E2/E3 for priority problems)
- Validate or invalidate operational assumptions about the approved Independent Mid-Size discovery segment in [Target Customer](../product/TARGET_CUSTOMER.md). Approved direction is not validated market truth.

### Scope decision outputs

- **MVP scope freeze** with explicit Product Owner approval (not yet done)
- Prioritize research areas into MVP, Next, Future, Integrate, or Reject using [Opportunity Matrix](../product/OPPORTUNITY_MATRIX.md)
- Refine [MVP Candidates](../product/MVP_CANDIDATES.md) into an approved scope document
- Confirm Build vs Integrate boundaries for pilot integrations → [Build vs Integrate](../product/BUILD_VS_INTEGRATE.md)

### Workflow and success criteria

- Document user workflows (workflow-oriented, not module-oriented)
- Establish success criteria for pilot validation
- Define pilot property profile and minimum integration dependencies

### Phase 1 does not include

- Application implementation
- Reopening accepted Sprint 0.3A ADRs without revisit conditions
- Defining final business module boundaries as a substitute for MVP scope freeze

---

## Phase 2 — Architecture & Technology Decisions

Sprint 0.3A accepted the architecture style and core stack (modular monolith, ASP.NET Core / .NET 10, React SPA, PostgreSQL, EF Core, REST + OpenAPI, Identity, permission-based authorization, provider-neutral cloud strategy).

Remaining Phase 2 work (still open):

- Define **business module boundaries** after MVP scope is approved (Phase 1)
- Evaluate multi-property requirements for architectural impact (do not implement tenant infrastructure)
- Select testing **frameworks** (testing *direction* is frozen; libraries are not)
- Select CI/CD approach
- Select a cloud **provider** when hosting evidence exists
- Record later ADRs for caching, brokers, background job libraries, external OIDC, and observability vendors if needed

All significant remaining decisions recorded as ADRs.

**No implementation should begin before a sprint authorizes it.** MVP scope freeze (Phase 1) remains independent: the accepted stack does not define product modules.

---

## Phase 3 — MVP Development

Implement approved MVP scope:

- Follow sprint lifecycle and development workflow
- Maintain change safety and module isolation principles
- Automated testing per testing strategy
- CTO code review and Product Owner acceptance before commits

Specific MVP features are defined only after Phase 1 approval—not in this document.

---

## Phase 4 — Pilot Hotel Validation

Validate MVP with real hotel operations:

- Deploy to pilot hotel environment
- Collect operational feedback using discovery and success criteria from Phase 1
- Produce E4 pilot evidence per [Evidence Model](../product/EVIDENCE_MODEL.md)
- Measure against success criteria defined in Phase 1
- Identify gaps, bugs, and prioritization for expansion

Early pilots exist to validate and improve HuGuWeb, not merely to prove that existing assumptions were correct. Evidence may change feature priority and product direction. See [Product Principles](../product/PRODUCT_PRINCIPLES.md).

---

## Phase 5 — Expansion

Expand product based on validated learnings:

- Prioritize Next and Future scope items from [Opportunity Matrix](../product/OPPORTUNITY_MATRIX.md)
- Evaluate integration partnerships per [Build vs Integrate](../product/BUILD_VS_INTEGRATE.md)
- Consider multi-property and hotel chain requirements (growth-direction hypothesis in [Target Customer](../product/TARGET_CUSTOMER.md); not a guaranteed commercial sequence)
- Evaluate employee mobile application (see [Future Scope](../product/FUTURE_SCOPE.md))
- Consider industry expansion only after hospitality MVP is validated

Specific expansion features are not defined in this document.

---

## Related Documents

- [Product Vision](../product/PRODUCT_VISION.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [Future Scope](../product/FUTURE_SCOPE.md)
- [Target Customer](../product/TARGET_CUSTOMER.md)
- [MVP Candidates](../product/MVP_CANDIDATES.md)
- [Evidence Model](../product/EVIDENCE_MODEL.md)
- [Engineering Principles](../engineering/ENGINEERING_PRINCIPLES.md)
- [Development Workflow](../engineering/DEVELOPMENT_WORKFLOW.md)
