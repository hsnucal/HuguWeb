# Personel List

> **Status:** Accepted — HR-01A. Extends the Sprint 0.8 Personel directory. Not implementation.

## Default columns

Operationally useful, not overwhelming. Align with the accepted 0.8 directory, plus photo.

| Column (TR) | Technical | Sensitivity | Default |
|-------------|-----------|-------------|---------|
| Fotoğraf | Photo / avatar | Normal (image is still PII) | Yes |
| Ad soyad | GivenName + FamilyName | Normal | Yes |
| Sicil no | PersonnelNumber | Normal | Yes |
| Departman | Current Department name | Normal | Yes |
| Pozisyon | Current Position name | Normal | Yes |
| İşe giriş | Employment.StartDate | Normal | Yes |
| Durum | EmploymentStatus | Normal | Yes |

Do **not** default TCKN, IBAN, home address, birth date, phone, or email.

Segments from 0.8 remain valid product direction: Aktif / Planlanan işe girişler / İşten ayrılanlar. Former staff stay on Personel; they are not deleted.

---

## Filters (HR-01B)

| Filter | Notes |
|--------|--------|
| Search | Name and personnel number (existing). Do not search TCKN unless the user has `hr.employee.sensitive.read` **and** a later explicit “sensitive search” is approved. HR-01B: no TCKN search. |
| Department | Existing |
| Position | Add |
| Employment status | Segments and/or explicit filter |
| Start-date range | Add |

Future filters (modules appear): working classification, official status, leave state — not now.

---

## Column picker

Configurable columns are accepted.

**Invariant:** column availability is the **intersection** of (a) catalog eligibility and (b) the user’s permissions / data classification.

A user with only `workforce.read` must not add TCKN, IBAN, or home address via the picker. Those columns are **Restricted**: they appear in the picker only with `hr.employee.sensitive.read`, and even then should stay off by default.

### Preference storage (MVP)

**Local UI preference** (browser), keyed by user id + list name.

Do **not** build a server-side column-preference service in HR-01B. Named “view profiles” on a server can wait until several hotels share machines or demand it.

If local storage is cleared, fall back to the default column set above.

---

## Row click

Opens the Personel Card overlay (HR-01B). Do not navigate to a GUID-looking URL that contains TCKN or IBAN. Employee technical id in the path is acceptable (`/personel?employee=:id` or overlay without a sensitive query string).

---

## Bulk actions on the list

| Action | Slice |
|--------|--------|
| Excel export | HR-01C (rules frozen now) |
| Excel import | HR-01C |
| Bulk photo | **Removed before HR-01C acceptance** — photos are managed individually on Personnel Card |
| Toplu Zam | **HR-09 Compensation** — not Personel Master |
| Generic bulk field edit | Reject |

Avoid a generic bulk-update engine. Each bulk operation is a named workflow with preview and permissions.

---

## Operational vs HR lists

`GET /api/workforce/active` and assignable-employee lists used by Oda Operasyonları / Teknik Servis are **not** the Personel List. They stay minimal operational references.

The Personel List is an HR surface behind `workforce.read` and/or `hr.employee.read` as defined in [PRIVACY_AND_PERMISSIONS.md](PRIVACY_AND_PERMISSIONS.md).
