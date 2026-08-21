# Domain Invariants

> **Status:** Accepted — Product Owner + CTO approved baseline. Only rules that protect business consistency.

Implementation belongs to Sprint 0.7B. Tests that should exist then are listed in [FIRST_SLICE.md](FIRST_SLICE.md). If an invariant would require premature infrastructure (brokers, government clients, notification tables), document the rule and do not invent that infrastructure.

---

## Identity

1. **PersonnelNumber is unique within Organization** among all Employee records, including former staff.
2. **PersonnelNumber is never reused** after employment ends (including for a different person).
3. **PersonnelNumber is not the database primary key.** Technical id stays stable if a number is corrected (rare, audited).
4. **Department and Position identity is the technical id**, not the display name. Names may change.

---

## Employment

5. **Employment period cannot be inverted.** End date, when present, is on or after start date.
6. **Ended employment has an end date.**
7. **An employee cannot have multiple simultaneous non-ended Employments** (`Scheduled` or `Active`).
8. **Ended Employment cannot receive a new Assignment.**
9. **Historical Employment records are retained.** Ending employment is not deletion. Employee is not deleted because employment ended.
10. **Employment state is not attendance state.** Leave, sickness, day off, shift, and “working today” are out of this model.
11. **Government-system status must not become Employment status.** SGK/KBS Pending/Submitted/Rejected/Failed are not Employment lifecycle values.

---

## Assignment

12. **Assignment period cannot be inverted.** End date, when present, is on or after start date.
13. **Assignment must belong to a valid Employment.**
14. **Primary Assignment period must fit within its Employment period.**
15. **Primary Assignments cannot overlap.** Transfer on date D: previous Primary ends D−1, new Primary starts D.
16. **Historical Assignment records are retained.** Transfer must not overwrite or delete previous assignments.
17. **New assignment cannot target an inactive Department.**
18. **New assignment cannot target an inactive Position.**

---

## Reference data

19. **Deactivating Department must not destroy historical references.** Deactivate; do not hard-delete once assignments exist.
20. **Deactivating Position must not destroy historical references.**

---

## Identity / authorization boundary

21. **Position does not grant permissions.**
22. **Department does not grant permissions.**
23. **Employee does not require ApplicationUser.** Hiring must not create a login.

---

## Official notifications (readiness, not implementation)

24. **Government notification failure must not erase valid internal workforce history.** A committed Hire or End Employment remains valid if SGK or KBS is down, rejects, or is not yet integrated.
25. HuGuWeb workforce commit and external government submission are **not** one distributed transaction.

Do not encode SGK/KBS payload rules, retry schedules, or authority-specific field requirements as workforce invariants. Those belong to a future integration slice.

---

## Explicitly not invariants yet

- Daily presence, leave, or sickness
- User account must exist
- Manager / reporting line
- Parent-department tree
- Temporary-assignment exclusivity rules beyond “not a second Primary”
- Multi-property assignment exclusivity
- Payroll calculation
- Automatic government submission

---

## Delete strategy

| Concept | Normal operation | Not |
|---------|------------------|-----|
| Organization / Property | Seeded; no delete in first slice | Tenant wipe |
| Department / Position | Deactivate; rename allowed | Generic `IsDeleted`; cascade-destroy history |
| Employee | Remains | Hard delete on termination |
| Employment | End-date + `Ended` | Delete row |
| Assignment | End-date | Delete row; overwrite current FKs on Employee |
| ApplicationUser | Separate Identity action, not in 0.7B | Cascade from employment |
