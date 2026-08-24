# HR-DOMAIN-003: Official Employment Data

## Status

**Accepted**

Accepted by Product Owner + CTO (2026-08-24). Sprint HR-03A — Resmî Bilgiler / Bildirge Kodları Discovery & Freeze.

**Accepted Product Owner amendment — 2026-08-24.** Product Owner reviewed the running HR-03B implementation before commit and expanded Resmî Bilgiler. Status remains **Accepted** (not reset to Proposed). The original freeze text below is retained; the amendment **supersedes** the listed items only. Unrelated HR-01 / HR-02 / HR-03C decisions are not reopened.

This record now authorizes HR-03B implementation **under the amendment**, not under a silent rewrite of the original freeze.

**HR-DOMAIN-001 remains Accepted and is not superseded.**
**HR-DOMAIN-002 remains Accepted and is not superseded.**

This record extends Organization & Workforce Foundation and Personel Master. It does not reopen Employee ≠ User, Position ≠ Permission, PersonnelNumber ≠ PK, Employment ≠ Attendance, government-status ≠ Employment status, or operational DTO minimization.

Approved product/domain direction is **not** a validated universal hotel truth. Reference-product observations are labeled as such.

---

## Accepted Product Owner amendment — 2026-08-24

These items were **deferred or rejected in the original freeze** and are now **IN** for HR-03B. They were **not** part of the original HR-03A freeze.

| Topic | Original freeze | Amendment |
|-------|-----------------|-----------|
| Görev Kodu | Out of 03B | **IN.** `EmploymentDutyCode` lookup. HuGu internal codes (not invented SGK numbers). Labels verified from WebİK. Not an authorization role. Not mapped from Position. |
| Occupation catalogue | No 7,765-row C# / application-source seed; practical bootstrap OK | **Full catalogue required.** Versioned **data artifact** `data/reference/sgk-occupation-codes.json` (extracted 7,765 rows from the Product Owner WebİK snapshot). Idempotent importer. Still **forbidden:** pasting 7,765 entries into `OfficialLookupCatalog.cs` or another C# array. |
| Occupation identity | Code on profile; name from catalogue | Unchanged and now enforced in UI: display `CODE — NAME`; search by code or name; persist code only. Position still does **not** own OccupationCode. |
| Resmî bilgiler | Bildirge Kodları section only | **Composition tab** with five sections: Bildirge Kodları → İŞKUR Aylık İşgücü Çizelgesi → BES → Sosyal Bilgiler → Eğitim Bilgileri. Composition ≠ one god object. |
| İŞKUR fields | Out (reports / çizelge) | **Master fields IN** on `Employment` (early HR-02-compatible contract/workforce facts). **İŞKUR report submission remains OUT.** `IskurWorkforceStatus` can duplicate Sözleşme Türü + İŞKUR Statüsü + disability; Product Owner still requires the field. |
| BES | Out with AGİ / payroll | **Configuration only** on `EmploymentBesSettings`: Kesinti, Oran %, Ek Tutar. **AGİ remains OUT.** No payroll calculation. Future HR-09/Payroll consumes these values. |
| Sosyal / eğitim | Out of this tab | **IN as composition.** Person-oriented social/education summary on `EmployeeHrProfile`. Work-permit dates on `Employment`. Organizational Bölüm is **not** an education field. Anne kızlık soyadı **excluded**. WebİK “Vize” labels become HuGu **Çalışma İzni**. |
| Uyruk | HR-01B free-text / “ISO later” | **ISO 3166-1 alpha-2** stored identity (`TR`, not “Türkiye”). Complete officially assigned set (249). Localized labels via `Intl.DisplayNames`. Column remains `varchar(64)`. |

**Still OUT (unchanged):** SGK/KBS/İŞKUR clients; SAVE = SUBMIT; payroll calculation; AGİ; mother’s maiden name; incentive % / 5510 flags; SGK notified checkboxes; exit codes; `hr.official.*`; operational DTO expansion; Room Operations / Technical Service changes.

**SAVE ≠ SUBMIT remains unchanged.**

---

## Context

HR-01A/01B delivered Personel Master: identity, contact, photo, Personel Card tabs Genel / Kimlik & iletişim / Çalışma / Geçmiş, and `hr.employee.*` permissions. Official codes were explicitly deferred to HR-03.

Personel Master field catalog already named four official employment codes as later work: belge türü, tabi kanun, sigorta kolu, meslek kodu. HR-03A freezes ownership, lookup strategy, Personel Card IA, and the first implementation slice.

A Product Owner–provided WebİK frontend snapshot was used as a **capability reference** (Bildirge Kodları fields, dropdowns, code/description patterns, company vs employee placement). HuGuWeb does not copy its source, branding, CSS, architecture, or assume that frontend validation is server-side or legal truth.

Current `Property` is thin (`Id`, `OrganizationId`, `Name`). Current `Employment` is period + status only and does **not** own `PropertyId`. Official workplace registration and employee statutory classification must not be dumped onto `Employee`. Employment/Assignment are **not** redesigned to attach Property.

---

## Boundary

**In:** Property-level SGK workplace registrations (0..*); Employment-level current statutory classification (`OfficialEmploymentProfile`, including optional `SgkWorkplaceRegistrationId`); lookup/reference-data principle for those code families; Personel Card Resmî bilgiler information architecture; requiredness classes; sensitivity; permission direction; HR-03B first-slice freeze; explicit non-submission boundary.

**Out of this record’s implementation (and out of HR-03B):** SGK işe giriş / işten ayrılış adapters; KBS; İŞKUR reports; notification obligation/result tables; credentials; workers/outbox/brokers; payroll incentives (5510/AGİ/BES); parent names; disability; identity booklet fields; EmploymentClassification (HR-02); exit/entry workflow codes as live End Employment fields; Property legal/accounting master (VKN, MERSİS, trade registry) beyond what workplace registration itself requires; Görev Kodu / DutyCode; effective-dated OfficialEmploymentClassification; embedding the full national occupation catalogue in application source.

---

## Decision

1. **Two ownership levels. Do not dump both onto Employee.**
   - **Employer / Property level:** official workplace registrations/configuration.
   - **Employment level:** employee-specific statutory classification for that work relationship, including which Property registration applies.
2. **A Property MAY have multiple SGK workplace registrations.** Conceptual type: `SgkWorkplaceRegistration` owned by Property as `Property → SgkWorkplaceRegistrations [0..*]`. Each registration is a separate business record. **Do not** impose a “one active registration per Property” domain invariant. Do not invent the complete registration field set in this freeze.
3. **OfficialEmploymentProfile may reference the applicable registration** (`SgkWorkplaceRegistrationId`). This avoids assuming that every Employment at one hotel/property belongs to the same SGK workplace. The registration’s `PropertyId` must correspond to the Property of the Employment’s relevant organizational context, established through the existing Assignment → Department → Property chain. Employment is **not** given a `PropertyId` for this purpose. See [EMPLOYMENT_OFFICIAL_PROFILE.md](EMPLOYMENT_OFFICIAL_PROFILE.md) and [INVARIANTS.md](INVARIANTS.md).
4. **Employee-specific official codes belong on Employment**, as `OfficialEmploymentProfile` (1:0..1 owned by Employment). They describe the employment period, not the person-for-life. Rehire is a new Employment and therefore a new profile.
5. **Bildirge Kodları is a section, not a top-level tab.** Visible Personel Card order becomes: Genel bilgiler → Kimlik & iletişim → Çalışma / organizasyon → **Resmî bilgiler** → Geçmiş. Unfinished official sections are not shown.
6. **Saving official fields is data foundation only.** It does not submit to SGK, KBS, or İŞKUR. Government notification obligation and submission result remain later, separate records (Accepted HR-DOMAIN-001 readiness). **SAVE ≠ SUBMIT.**
7. **New Personnel creation does not require Resmî bilgiler.** Minimum hire remains HR-01B (name, sicil, start date, department, position). All Bildirge Kodları fields stay optional. Official fields are completed afterward on the card. Personel Card save must **not** infer SGK submission completeness.
8. **Edit surface is Personel Card → Resmî bilgiler → Bildirge Kodları.** That section does not transfer department, change Position, end Employment, submit SGK, or perform KBS notification. **SGK İşyeri on the card selects an existing registration**; it does not create one by typing a workplace number. Property/organization configuration is the create/edit surface for registrations. Configuration UI is **not** implemented in HR-03A.
9. **Controlled codes are lookups, not free text.** Explicit lookup families: `SgkDocumentType`, `ApplicableLawCode`, `InsuranceBranch`, `SgkOccupationCode` catalogue. Do **not** invent a generic `GovernmentCode` table. Store the stable **code** as identity; labels/descriptions are reference metadata.
10. **Store the official code, not the concatenated display string.** UI shows `code + description`. Persistence stores the code. Reference product stored `"01 - AYLIK SİGORTA PRİM BİLDİRGESİ"` as the field value — HuGuWeb rejects that as the stored identity.
11. **Position ≠ SGK Occupation Code.** `OfficialEmploymentProfile` owns/selects `OccupationCode`. Do **not** attach authoritative SGK occupation identity to Position in HR-03B. A later Position recommended/default occupation code is **FUTURE / NEEDS VALIDATION** and, if ever implemented, is **suggestion only** — never automatic statutory truth.
12. **Do not seed the full 7,765-row occupation catalogue into source-controlled application seed data.** Preferred architecture: a maintained/importable reference catalogue. HR-03B may implement the reference structure and a practical bootstrap/import strategy. Catalogue versioning and update are first-class concerns (see [LOOKUP_CODES.md](LOOKUP_CODES.md)).
13. **HR-03B uses a current-value `OfficialEmploymentProfile` per Employment.** Do **not** implement effective-dated `OfficialEmploymentClassification` records in HR-03B. Rehire remains naturally separated. Mid-employment statutory-code history remains deferred. **Architectural requirement:** HR-03B must not make a later migration to effective-dated statutory history unreasonably difficult.
14. **Görev Kodu / DutyCode is out of the HR-03B minimum scope.** WebİK exposes the field with six Turkish labels; that does **not** establish a stable official statutory code catalogue required by our SGK model. Classification: **DEFERRED / NEEDS DOMAIN OR LEGAL VALIDATION**. Discovery evidence is retained. HuGuWeb does not yet model it. It can be added later if validated. Do **not** show it on the Personel Card in 03B.
15. **SGK exit codes, İŞKUR exit codes, and departure reason belong to Employment end workflow (HR-02 / later official notification lifecycle), not OfficialEmploymentProfile and not Employee.** Reference product placed them on the Bildirge tab; HuGuWeb does not.
16. **Existing `hr.employee.read` / `hr.employee.manage` / `hr.employee.sensitive.read` are sufficient for the first employee-official slice.** Do not add `hr.official.*`. Property SGK registration management remains a **configuration** concern (`workforce.manage`; no Property admin product exists today). No DB-managed roles.
17. **Do not expose official fields through `OperationalEmployeeReference`, Room Operations, or Technical Service.** Workplace registration, document type, applicable law, insurance branch, and occupation code stay out of operational DTOs.
18. **Parent names, disability, İŞKUR çizelge fields, KBS statuses, payroll incentives, and SGK notified checkboxes are not this slice’s Personel Card section.** They remain later official / payroll / notification work already mapped in Personel Master. Do not add empty KBS / İŞKUR / SGK submission sections.

---

## Key decisions

| Topic | Choice |
|-------|--------|
| Relationship to Workforce / Personel Master | Extend; do not replace. Do not redesign Employment/Assignment. |
| Property official data | `SgkWorkplaceRegistration` collection on Property: **0..\*** |
| One-active-per-Property invariant | **Rejected.** Not imposed. |
| Employee official data | `OfficialEmploymentProfile` 1:0..1 on Employment (current-value) |
| Employment → SGK workplace | `OfficialEmploymentProfile.SgkWorkplaceRegistrationId` (optional). Validity via Assignment → Department → Property. |
| Belge türü / Tabi kanun / Sigorta kolu | Explicit lookup types; store code |
| Meslek kodu | Owned by OfficialEmploymentProfile; code only + importable catalogue; **not** full seed in source |
| Position → meslek kodu | Position ≠ occupation code. Optional Position default is FUTURE / suggestion only |
| Görev Kodu | **Out of 03B.** DEFERRED / NEEDS DOMAIN OR LEGAL VALIDATION. Evidence retained. |
| Effective-dated history | **Not in 03B.** Snapshot per Employment. Must remain migratable later. |
| Entry/exit official codes | HR-02 / later notification lifecycle |
| SGK notified flags | Reject on profile; later notification record |
| Create-hire requiredness | All Bildirge Kodları optional. Card save ≠ SGK completeness. |
| Permissions | Existing `hr.employee.*`. Property registrations = configuration (`workforce.manage`). No `hr.official.*`. |
| SGK/KBS/İŞKUR clients | Out. SAVE ≠ SUBMIT. |
| First production slice | [FIRST_SLICE.md](FIRST_SLICE.md) HR-03B |

---

## Rejected alternatives

| Alternative | Why |
|-------------|-----|
| Official columns on Employee | Mixes person identity with employment-period classification; breaks rehire |
| Official columns directly on Employment | Works for a few fields, but lookup FKs and later growth belong in an owned profile (same pattern as EmployeeHrProfile) |
| Effective-dated OfficialEmploymentClassification now | Correct later if monthly SGK needs intra-employment history; too large for the data foundation. 03B must still leave a migration path. |
| Bildirge Kodları as a top-level tab | Contradicts accepted Personel Card IA; Resmî bilgiler is the tab |
| Duplicate işyeri sicil typed on every employment | Property owns workplace registrations; the card **selects** an existing record |
| One active SGK registration per Property as a domain invariant | A Property may have several concurrent registrations (hotel vs restaurant vs spa, etc.) |
| Full legal master on Property (VKN, MERSİS, trade registry, KEP, KBS credentials) | Out of this function; Organization/Property stay thin |
| Generic OfficialCode / GovernmentCode framework | Only a few code families exist |
| Seed 7765 meslek rows in application source | Catalogue must be maintained/imported; releases must not embed thousands of official codes |
| Authoritative meslek kodu on Position | Position ≠ SGK Occupation Code |
| Save = SGK submit | Contradicts Accepted government-readiness (separate transaction) |
| SGK giriş/çıkış bildirildi checkboxes on the card | Notification state is not master data |
| New `hr.official.*` permission family in 03B | Unjustified proliferation |
| Empty İŞKUR / KBS / parent-name / SGK submission sections on the tab | Hide until owning slice |
| DutyCode in 03B minimum | WebİK labels do not prove a required statutory catalogue |
| Redesign Employment to own PropertyId | Existing Assignment → Department → Property already establishes organizational context |

---

## Risks

| Risk | Mitigation |
|------|------------|
| One Property needs several SGK işyeri (hotel vs restaurant vs spa) | **Decided:** collection 0..*; profile selects `SgkWorkplaceRegistrationId` |
| Experts require hire-time codes to remain after a mid-employment meslek change | Snapshot-per-Employment is the 03B floor; keep the profile a dedicated owned record so an effective-dated type can be added later without rewriting Employee |
| Legal requiredness is stricter than WebİK’s empty “Seçiniz” | Requiredness classes: REFERENCE vs HUGUWEB vs OPTIONAL vs EXPERT. Card save stays optional. Completeness is a future submit workflow. |
| VKN/tax later needed for e-notification headers | Keep on a future Organization legal profile, not copied onto every employee |
| Official data leaks to floor modules | Freeze operational reference; no new fields on AssignableEmployee |
| Occupation catalogue goes stale | Importable/versioned reference data; do not freeze 7765 rows in git |
| Mid-employment change of SGK workplace | Current profile FK can be overwritten in 03B; whether that is legally a new workplace period remains an expert question |

---

## Date

2026-08-24

---

## Related Documents

- [README.md](README.md)
- [HR-DOMAIN-001](../HR-DOMAIN-001-Organization-Workforce-Foundation.md)
- [HR-DOMAIN-002](../personnel-master/HR-DOMAIN-002-Personnel-Master.md)
- [FIELD_CATALOG.md](FIELD_CATALOG.md)
- [FIRST_SLICE.md](FIRST_SLICE.md)
