# Personel Master invariants

> **Status:** Accepted — HR-01A. Adds to Accepted [workforce invariants](../INVARIANTS.md). Does not replace them.

Implementation belongs to HR-01B/01C. If an invariant would require payroll, government clients, or a document store, document the rule and do not invent that infrastructure.

---

## Identity and numbering

1. Accepted PersonnelNumber rules still hold: unique within Organization, never reused, not the PK.
2. **National identity number is not the PK, not PersonnelNumber, not login, not a cross-domain public id.**
3. When a national identity number is present, uniqueness is conceptual **Organization + Scheme + normalized identifier** (including former staff).
4. National identity number **may be absent** (foreign staff, incomplete intake). Absence is valid.
5. Algorithm checks (TCKN checksum, IBAN checksum) **do not prove legal identity**. They only reject malformed input.
6. A personnel-number correction does not change `EmployeeId`. It is a rare, historically notable change.

---

## Profile vs employment

7. Profile edits do not create or end Employment.
8. Changing Department/Position on the card is **Transfer**, not a profile save.
9. Employment start/end period rules remain the Accepted period invariants.
10. `OriginalCompanyStartDate` / `SeniorityStartDate` wait for **HR-02**. When present later, they do not replace `Employment.StartDate` as the relationship start.
11. Government notification timestamps/status **must not** become Employment status (Accepted invariant 11). They must not become Employee fields either.

---

## Sensitivity and exposure

12. Operational modules receive only `OperationalEmployeeReference`.
13. Highly sensitive fields are omitted from default list, default export, and `workforce.read` payloads.
14. Column picker cannot grant data the user’s permissions do not allow.
15. TCKN and IBAN must not appear in URLs or routine logs.

---

## Photo

16. Employee does not store image bytes as a string column.
17. Replacing a photo archives or deletes the previous storage object; one current photo per employee.
18. Bulk photo matches PersonnelNumber first.

---

## Delete

19. **Employee is never deleted as part of termination.** Employment end is not Employee deletion (Accepted).
20. Personel Card has no normal “Personeli Sil” for a person with employment history.
21. Exceptional erasure belongs to a future privacy / data-governance process, not the HR card.

---

## Import

22. Import cannot persist derived fields as if they were inputs.
23. Import must not commit a partial batch without a result report.
24. Import of Department/Position must use references, not unconstrained strings that bypass inactive-target rules.

---

## Validations (when implemented)

| Field | Rule |
|-------|------|
| PersonnelNumber | Required; trim; max length (existing); unique in Organization; never reused |
| GivenName / FamilyName | Required; trim; max length; no phonetic/script theatrics |
| NationalIdentityNumber | Optional; if `Tckn`, 11-digit checksum; if `Ykn`/`Passport`/`Other`, non-empty trimmed string + scheme; unique as Organization + Scheme + normalized identifier when present. Checksum ≠ legal identity |
| IBAN | Optional until 01C; when present, checksum/format; store compact |
| Email | Optional; format only |
| BirthDate | Optional; not a future date; reject absurd historical dates; no minimum-age hotel policy in code |
| Employment dates | Accepted period rules; End ≥ Start; at most one non-ended employment |
| Phone | Trim; store digits with optional leading +; no inventing a full telephony product |
| Photo | Allowed image types; size cap; replace is explicit |
| EmergencyContact.Phone | Same as phone; Name required if the row is kept |

---

## Explicitly not invariants yet

- TCKN required
- IBAN required
- Blood type required
- Grade required
- Working Group exists
- Emergency contact required
- Encryption-at-rest field policy
- Sensitive-access audit log
- Automatic sicil numbering (still manual per HR-DOMAIN-001)
