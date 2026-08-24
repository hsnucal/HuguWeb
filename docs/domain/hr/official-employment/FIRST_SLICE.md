# First implementation slice

> **Status:** Accepted freeze for **HR-03B** / **HR-03C+**. **Accepted Product Owner amendment — 2026-08-24** expands the HR-03B Personel Card composition before commit. Status remains Accepted.

Official Employment Data **model** is larger than the first production slice. Do not make HR-03B an SGK product. SAVE still does not submit to SGK, İŞKUR, or KBS, and does not calculate payroll.

---

## Quality bar (must stay true after 03B)

1. Property owns workplace registrations as a **0..\*** collection; Employment owns a current statutory profile that may reference one applicable registration; Employee does not own either dump.
2. There is **no** one-active-registration-per-Property invariant.
3. Saving Bildirge Kodları does not submit to any government system. SAVE ≠ SUBMIT.
4. Personel Card visible tabs: Genel, Kimlik & iletişim, Çalışma, Resmî bilgiler, Geçmiş. Bildirge Kodları is a section.
5. Hire remains HR-01B-minimum; all Bildirge Kodları optional. Card save does not infer SGK completeness.
6. Lookups are explicit families; meslek is code + **importable JSON artefact** (full snapshot catalogue), never a 7,765-row C# array.
7. Position ≠ SGK occupation code.
8. Görev Kodu is **IN** (PO amendment 2026-08-24). Original freeze had it out.
9. Current `OfficialEmploymentProfile` 0..1 per Employment; effective-dated history deferred but must remain migratable.
10. Operational DTOs unchanged — no workplace registration, document type, applicable law, insurance branch, occupation, duty code, İŞKUR, BES, military, passport, KEP, work permits, education, or nationality.
11. Existing `hr.employee.*` permissions; no `hr.official.*`. Property registrations are configuration (`workforce.manage`).
12. HR-DOMAIN-001 and HR-DOMAIN-002 remain Accepted.
13. TR/EN/RU for new UI strings.
14. This document is the 03B contract.

---

## HR-03B — recommended production slice

### In

- Personel Card **Resmî bilgiler** tab (visible).
- **Bildirge Kodları** section fields:
  - SGK İşyeri (select existing `SgkWorkplaceRegistration`)
  - Belge türü
  - Tabi olduğu kanun
  - Sigorta kolu
  - SGK meslek kodu (searchable; **full** catalogue via JSON artifact + importer)
  - Görev Kodu (PO amendment)
- Composition sections on the same tab (PO amendment; not a god object): İŞKUR Aylık İşgücü Çizelgesi, BES (config only, no AGİ), Sosyal Bilgiler, Eğitim Bilgileri.
- `OfficialEmploymentProfile` 1:0..1 owned by Employment (current-value), plus `DutyCode`.
- Profile fields: `EmploymentId`, `SgkWorkplaceRegistrationId`, `DocumentTypeCode`, `ApplicableLawCode`, `InsuranceBranchCode`, `OccupationCode`, `DutyCode`.
- Lookup tables: `SgkDocumentType`, `ApplicableLawCode`, `InsuranceBranch`, `SgkOccupationCode`, `EmploymentDutyCode`.
- Occupation: versioned data artifact `data/reference/sgk-occupation-codes.json` + idempotent importer. **Not** 7,765 rows in a C# array. **Not** a competing 6-row bootstrap.
- `SgkWorkplaceRegistration` collection on Property (**0..\***). Configuration editor (not Personel Card typing) so registrations exist to be selected. Permission: `workforce.manage`.
- Validity: when `SgkWorkplaceRegistrationId` is set, `registration.PropertyId` must match the Employment’s relevant Property via current/last Primary Assignment → Department.
- Validations: lookup membership; occupation format when present; registration Property correspondence; do not invent legal regex for sicil beyond “non-empty trimmed” + optional digit-length **hint**.
- Unsaved-changes guard includes the new section.
- Tests for ownership, optional-at-hire, lookup validation, registration Property invariant, operational DTO non-expansion, no one-active-per-Property uniqueness.
- Development persona claims unchanged unless a test user must edit Property registration (`workforce.manage` already on HR manager).

### Out of HR-03B

- SGK işe giriş / işten ayrılış HTTP/XML clients
- KBS / Jandarma clients
- İŞKUR **report submission** (master İŞKUR fields are IN per PO amendment)
- Notification obligation/result tables
- Credentials, workers, outbox, brokers, retry
- Submission statuses
- Exit codes on the card
- AGİ and payroll incentive **rates** (BES configuration dates/flags: Kesinti / Oran / Ek Tutar are IN; 5510 % fields remain OUT)
- Parent names (including Anne kızlık soyadı), disability, booklet fields
- Effective-dated OfficialEmploymentClassification
- Position.DefaultOccupationCode (and any automatic occupation write)
- Full 7,765-row occupation catalogue **in a C# array** (JSON artifact is required)
- One-active-per-Property uniqueness
- VKN/MERSİS legal master
- `hr.official.*` permissions
- DB-managed authorization
- Empty placeholder KBS / İŞKUR / SGK submission sections
- Technical Service / Room Operations changes
- Excel import of official codes (Personel Master import is HR-01C; official columns later Restricted)
- Configuration UI in **HR-03A** (this folder is documentation only)

HR-03B **will not**: send SGK işe giriş; send SGK işten çıkış; create submission statuses; create notification jobs; add credentials; add adapters; add retry infrastructure; add outbox/broker/worker.

---

## HR-03C / later — deferred

| Item | Notes |
|------|--------|
| SGK işe giriş submission | Adapter consumes Employment + profile + selected Property registration + Personel Master identity. Completeness validation lives here, not on card save. |
| SGK işten ayrılış submission | Needs HR-02 end workflow + EK-2 code |
| Notification records | Pending/Submitted/Accepted/Rejected — not Employment status |
| Retry / status infrastructure | Evidence-driven when a real adapter exists |
| KBS | Personel Master already has identity/address prerequisites; do not add statuses in 03B |
| İŞKUR **report submission** | Master İŞKUR fields are IN (PO amendment). Monthly chart filing / XML remains later |
| Intra-employment classification history | Option C; 03B profile must remain a viable migration source |
| Position recommended occupation | FUTURE / suggestion only if ever added |
| Government credentials vault | Not Property columns in git |

---

## Dependency on HR-02

Roadmap currently places HR-02 Entry/Exit before HR-03 Official. That remains the conceptual product sequence for **lifecycle** (exit reason, rehire UX, EmploymentClassification, seniority dates).

| Question | Answer |
|----------|--------|
| Can official profile persist before HR-02? | **Yes.** `OfficialEmploymentProfile` attaches to existing Employment. No new employment lifecycle is required. |
| Which fields depend on finalized Entry/Exit? | SGK/İŞKUR **exit codes**, departure reason, suspend, original-company/seniority dates, first-class EmploymentClassification. |
| What is safe independent configuration? | Property SGK registrations, lookup catalogues, belge/kanun/kolu/meslek + SGK İşyeri on the employment profile, Resmî bilgiler tab. |

**Do not silently reorder the roadmap.** HR-03A discovery proceeding now is a documentation freeze, not HR-02 skipped. HR-03B **may** be implemented without waiting for HR-02 **if** Product Owner wants official master data before exit-workflow polish — provided exit codes stay out of 03B. HR-01C (Excel/IBAN) is unrelated and may remain parallel/later.

HR-03 still must not start **implementation** before HR-01B identity exists — that prerequisite is already satisfied on `main`.

---

## Validation categories (03B)

| Category | Rule |
|----------|------|
| Lookup | Value empty OR code exists in the family table |
| Occupation format | If present, 7-char `NNNN.NN` as used by the catalogue |
| Employment scope | Profile EmploymentId must be a real Employment of that Employee |
| Registration Property | If `SgkWorkplaceRegistrationId` present: registration exists and `PropertyId` matches current/last Primary Assignment’s Department.PropertyId |
| Property registration number | RegistrationNumber trimmed; no speculative checksum |
| Dates | Workplace ValidFrom/To, if used later, must not invert; not required in 03B minimum field set |
| Hire | Official fields ignored by Hire required set |

Do not encode SGK payload completeness as a Personel Card save error in 03B.

---

## Expert validation questions

Decided questions are **removed** from this list (multiple SGK registrations per Property; Position vs occupation code; occupation catalogue source strategy; current profile vs effective-dated model for HR-03B).

Keep only questions that still change later design:

1. **Which official fields are mandatory before an actual SGK işe giriş notification may be sent?** Changes HUGUWEB REQUIRED vs OPTIONAL at submit time; must not be inferred from WebİK’s empty Seçiniz or from Personel Card save.
2. **Can one Employment change SGK workplace mid-employment without ending Employment?** 03B can overwrite the current FK. If legal/historical workplace periods are required, Option C / workplace history is needed.
3. **Does Görev Kodu have statutory significance** (stable official catalogue required by the SGK model), or is it only a WebİK label set? Changes whether DutyCode returns after 03B.
4. **Do seasonal / retired / intern need first-class `EmploymentClassification` (HR-02), or is belge türü enough?** Changes whether 03B and HR-02 both model the same fact.
5. **Does employer VKN belong on Organization (later legal profile) rather than Property?** Prevents Property legal-master creep; may still be needed on a future notification header.
6. **Is legacy employee “Sos. Güv. No” still required besides TCKN?** Changes whether a person-level SocialSecurityNumber returns in a later official-identity slice.

Bootstrap vs empty occupation table at first run is an **HR-03B implementation choice** under the decided importable-catalogue architecture, not an open domain fork.

---

## Closed by HR-03A acceptance

| Question | Decision |
|----------|----------|
| Tab vs section | Resmî bilgiler tab; Bildirge Kodları section |
| Property vs Employee sicil | Property `SgkWorkplaceRegistration` **0..\*** |
| One-active-per-Property | **Rejected** |
| Employment vs Employee codes | `OfficialEmploymentProfile` 1:0..1 current-value |
| Employment → workplace | Optional `SgkWorkplaceRegistrationId`; validity via Assignment → Department → Property |
| Position vs meslek kodu | Position ≠ occupation code. Profile owns OccupationCode. Position default is FUTURE / suggestion only |
| Meslek catalogue | Importable/maintained reference; not full seed in source |
| Görev Kodu | **IN** as of PO amendment 2026-08-24 (`EmploymentDutyCode`). Original freeze had it deferred. |
| History | Snapshot per Employment in 03B; effective-dated deferred; must remain migratable |
| Permissions | Existing `hr.employee.*`; Property registrations = configuration |
| Save vs submit | Save is not submit |
