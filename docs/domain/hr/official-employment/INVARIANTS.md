# Official employment invariants

> **Status:** Accepted — HR-03A. Adds to Accepted workforce and Personel Master invariants. Does not replace them.

Implementation belongs to HR-03B. If an invariant would require SGK/KBS clients, notification tables, or brokers, document the rule and do not invent that infrastructure.

---

## Ownership

1. **SGK işyeri sicil is not stored on Employee and is not typed as a free-text copy on every Employment.** Property-owned `SgkWorkplaceRegistration` is the source of truth. OfficialEmploymentProfile may **reference** one applicable registration (`SgkWorkplaceRegistrationId`).
2. **A Property MAY have multiple `SgkWorkplaceRegistration` records (0..\*).** Each registration is a separate business record. **There is no “one active registration per Property” invariant.**
3. **Statutory employee codes are not stored on Employee.** They live on `OfficialEmploymentProfile` for a specific Employment.
4. **Position does not equal Meslek kodu.** Transfer/rename of Position does not rewrite OccupationCode. Position does not own authoritative SGK occupation identity.
5. **Assignment does not own official codes.**

---

## Employment → registration validity (existing Workforce path)

Employment does **not** own `PropertyId`. Do not add it for this slice.

Relevant organizational context is established by the Accepted model:

```text
Employment → Primary Assignment → Department.PropertyId → Property
                                 → Position.PropertyId   → Property
```

6. **When `OfficialEmploymentProfile.SgkWorkplaceRegistrationId` is set:**
   - the registration must exist;
   - `SgkWorkplaceRegistration.PropertyId` must equal the Property of the Employment’s relevant organizational context;
   - **relevant context** = `Department.PropertyId` of the current Primary Assignment if the Employment is not ended; if ended, `Department.PropertyId` of the last Primary Assignment.
7. **Do not redesign Employment or Assignment** to carry Property or SGK workplace. Hire already creates a Primary Assignment, so the chain exists for every persisted Employment.
8. **Personel Card does not create registrations.** It only selects an existing registration belonging to that Property.

Position.PropertyId of the same Assignment is already property-scoped in the Accepted model and is expected to match Department.PropertyId. That is an existing organizational fact, not a new official-employment rule.

---

## Persistence and identity of codes

9. **When a controlled code is present, it must exist in its lookup** (active or historically valid). Free text is rejected for belge türü, tabi kanun, sigorta kolu, meslek kodu. Görev kodu is not a 03B stored field.
10. **Stored identity is the code**, not `code + " - " + description`.
11. **Lookup deactivation does not rewrite historical profiles.** Ended Employment keeps the codes it had.
12. **OccupationCode, when present, matches the catalogue format** used by the lookup (source: 7-character `NNNN.NN`). Do not invent extra legal regexes.
13. **The occupation catalogue is a maintained/importable reference**, not a 7,765-row application source seed. Updating catalogue descriptions must not rewrite stored profile codes.

---

## Lifecycle

14. **Profile create/update does not Hire, Transfer, or End Employment.**
15. **Ending Employment does not delete OfficialEmploymentProfile.**
16. **At most one OfficialEmploymentProfile per Employment** in 03B (current-value snapshot).
17. **Rehire is a new Employment and therefore a new profile.** Mid-employment statutory-code history is not modeled in 03B.
18. **HR-03B must not make a later migration to effective-dated statutory history unreasonably difficult.** Keep official codes on the dedicated owned profile; store stable codes; do not denormalize them onto Employee, Assignment, or operational DTOs.
19. **Government notification status is not written onto the profile** (Accepted workforce invariant 11 still holds).

---

## Submission boundary

20. **Saving Resmî bilgiler is not an SGK, KBS, or İŞKUR submission.**
21. **HuGuWeb commit and external government submission are not one transaction** (Accepted).
22. **No credentials are stored on official profile or workplace registration in this slice.**
23. **Personel Card save does not infer SGK submission completeness.** All Bildirge Kodları fields remain optional for HR-01B hire and ordinary card save.

---

## Exposure

24. Operational modules receive only `OperationalEmployeeReference`.
25. **`OperationalEmployeeReference` does not receive** workplace registration, document type, applicable law, insurance branch, or occupation code.
26. Room Operations and Technical Service remain unaware of these fields.
27. Personel List / export follow the same permission classes as the card.
28. Official codes are not placed in the Personel Card header.

---

## Explicitly not invariants yet

- Official fields required at hire
- Meslek kodu required before Employment can be Active
- Legal requiredness before an actual SGK submit
- Effective-dated intra-employment classification history
- Whether an open Employment may change SGK workplace (03B may overwrite the current FK; historical workplace periods are not modeled)
- Whether Görev Kodu has statutory significance
- Whether HR-02 `EmploymentClassification` overlaps belge türü enough to forbid a second concept
- VKN exists on Organization
- Automatic government submission
- At most one active SgkWorkplaceRegistration per Property (**rejected**)
