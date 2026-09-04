# Product Principles

These principles guide HuGuWeb product decisions during discovery and beyond. They are not a feature list or MVP commitment.

---

## Solve problems, not feature checklists

Competitor features are research inputs, not automatic requirements.

HuGuWeb should not attempt to copy every feature from existing ERP and PMS platforms. Competitor products inform understanding of the market; they do not define HuGuWeb's scope.

For every major feature under consideration, evaluate:

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

Possible feature decisions may eventually include: **MVP**, **Next**, **Future**, **Integrate**, or **Reject**.

Do not classify current features yet unless already explicitly stated.

---

## Don't show modules. Show work.

Traditional ERP systems often expose internal module structure directly to users.

HuGuWeb should investigate workflow-oriented UX. Design future experiences around what users need to accomplish—not around forcing users to understand ERP module boundaries.

Examples:

| Traditional thinking | Workflow-oriented thinking |
|---------------------|---------------------------|
| Housekeeping Module | Which rooms need attention? |
| Business Intelligence Module | What requires my attention today? |

This is a product/UX principle. It does not authorize UI design during the foundation stage.

---

## Hotel-first

Hotel workflows drive early product decisions.

HuGuWeb is designed for hotels and hospitality as the first target industry. Do not design the current system around hypothetical future industries. Expansion into other sectors may be considered later, but early decisions must reflect real hotel operational needs.

---

## Every active employee is identity-capable

HuGuWeb is an employee-facing platform. An active employee should be able to authenticate. That is not the same as granting HR permissions. `Employee` remains workforce identity; `ApplicationUser` remains authentication identity; `EmployeeAccountLink` connects them. See [Employee identity access](../domain/hr/EMPLOYEE_IDENTITY_ACCESS.md).

---

## Simplicity is a feature

Complexity must provide measurable value.

Operational simplicity is a product goal. Adding features, screens, configuration options, or integrations without clear user or business benefit increases maintenance cost and user burden.

---

## Integrate when building is not strategic

Do not recreate mature external platforms without strong justification.

HuGuWeb should evaluate **Build vs Integrate** for capabilities where specialized external systems already exist and are widely adopted. Examples include channel managers, OTA integrations, payment providers, POS systems, e-Invoice/e-Archive, government reporting integrations, payroll systems, and revenue management systems.

Integration vendors are not selected during the foundation stage.

---

## Evidence before scope

Major features require evidence of business and user value.

Scope decisions should be supported by research, user feedback, operational analysis, or other evidence—not by assumption, competitor parity, or internal preference alone.

Approved discovery direction is not the same as validated market truth. See [Target Customer](TARGET_CUSTOMER.md) and [Evidence Model](EVIDENCE_MODEL.md).

---

## Pilots exist to learn, not to confirm the plan

> Early pilots exist to validate and improve HuGuWeb, not merely to prove that existing assumptions were correct.

Pilot feedback may:

- increase feature priority
- decrease feature priority
- reveal missing workflows
- invalidate assumptions
- expose usability problems
- expose integration requirements
- reveal operational risks

Do not preserve a feature simply because it appeared in an earlier plan. Evidence should be allowed to change product direction.

---

## Reliability over perfection

Avoid claiming HuGuWeb will become a "bug-free" or "perfect" ERP.

Prefer product and engineering goals such as:

- high reliability
- predictable behavior
- controlled change impact
- strong regression protection
- observable failures
- rapid diagnosis
- safe recovery
- continuous improvement from pilot evidence

These are goals, not an architecture or implementation plan.

---

## Challenge assumptions

Product Owner, CTO, and implementation assumptions can all be challenged.

HuGuWeb is developed using a Product Owner + CTO decision model:

- The Product Owner does **not** automatically dictate implementation.
- The CTO does **not** automatically dictate product scope.

Both sides are expected to challenge decisions when necessary. Decisions should be based on evidence and explicit reasoning.

---

## Related Documents

- [Product Vision](PRODUCT_VISION.md)
- [Target Customer](TARGET_CUSTOMER.md)
- [Evidence Model](EVIDENCE_MODEL.md)
- [Future Scope](FUTURE_SCOPE.md)
- [Competitor Analysis](../research/COMPETITOR_ANALYSIS.md)
- [Development Workflow](../engineering/DEVELOPMENT_WORKFLOW.md)
