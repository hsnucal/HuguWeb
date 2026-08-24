# Personel Card

> **Status:** Accepted — HR-01A. Information architecture and behavior freeze. Not visual design. Not implementation.

## Product direction

```text
Personel list  →  click employee  →  large modal / overlay workspace
Yeni Personel  →  same Personel Card in CREATE MODE
Existing       →  same Personel Card in EDIT or READ MODE
```

Create and edit share one card model. Do not ship a separate “hire wizard” as the long-term Personel Master surface. Existing Hire / Transfer / End Employment **commands** remain the employment-lifecycle writes; the card composes them.

Detail-as-full-page (Sprint 0.8) may remain until HR-01B replaces it with the overlay. HR-01B should not keep two competing edit surfaces.

---

## Header (always visible)

| Element | Source |
|---------|--------|
| Photo / avatar fallback | EmployeePhoto, else initials |
| Given name + family name | Employee |
| Personnel number | Employee |
| Employment status | Employment |
| Department | Current Primary Assignment |
| Position | Current Primary Assignment |
| Employment start date | Employment.StartDate |

Do **not** expose GUIDs. Do **not** put TCKN, IBAN, or SGK badges in the header.

Reference product also showed SGK notified chips and portal status in the header. **Reject for HuGuWeb header.** Official status is not Employment status and must not decorate identity.

---

## Unsaved-changes guard (accepted UX invariant)

If the user has modified Personel Card data and attempts:

- ✕
- Cancel / Close
- Escape
- any navigation that would close the card

the product **must not silently discard** modifications.

Detect dirtiness by comparing current values to the last loaded/saved snapshot — not by “field was focused.” After a successful save, the snapshot resets.

Do **not** implement this in HR-01A.

**REFERENCE PRODUCT OBSERVATION:** WebİK uses one close path for ✕ / Close / Escape, compares serialized form to the opening snapshot, and also hooks `beforeunload`. HuGuWeb should take the UX lesson, not the implementation.

---

## Tab information architecture

### HR-01 Personel Card tabs (frozen)

| # | Tab (TR) | EN direction | Owns / composes | HR-01B |
|---|----------|--------------|-----------------|--------|
| 1 | Genel Bilgiler | General | Name, sicil, photo, education summary, optional notes, read-only org snapshot | **Yes** |
| 2 | Kimlik & İletişim | Identity & contact | National identity, demographics, addresses, phones, email, emergency contacts | **Yes** |
| 3 | Çalışma / Organizasyon | Work / organization | Employment + current/last assignment; hire/transfer/end actions by permission | **Yes** (compose existing) |
| 4 | Ücret & Ödeme | Pay & payment | Payment profile + later compensation | Placeholder only; persist pay in HR-01C / HR-09 |
| 5 | Resmî Bilgiler | Official | SGK / KBS / İŞKUR **master fields and later statuses** | Placeholder; data in HR-03 |
| 6 | Belgeler | Documents | Uploaded özlük files | Placeholder; HR-04 |
| 7 | Evraklar | Forms | Generated printable HR forms | Placeholder; HR-04 |
| 8 | Geçmiş | History | Employment + assignment history now; profile-change history later | **Yes** for workforce history |

Tabs 4–7 may be **hidden** until their slice, rather than empty shells. Empty shells teach users that HuGuWeb is unfinished. Prefer hide until the owning slice.

### Future / conditional — prefer module screens

| Topic | Card tab? | Recommendation |
|-------|-----------|----------------|
| Eğitim (detailed) | Optional later summary | Training / career module is source of truth; Personel Master holds **education level** only |
| Performans | No in HR-01 | Separate screen; optional summary tab much later |
| Kariyer | No | Includes Grade if ever added |
| Disiplin | No | Separate |
| Zimmet | No | Assets module |
| İzin / Puantaj summaries | Optional later | Leave/attendance own balances |
| DISC | **Never** | Out of scope |

### Belgeler vs Evraklar

Keep the distinction for later HR-04:

- **Belgeler** — uploaded files (contract, identity scan, health report).
- **Evraklar** — generated/print-fill forms (exit papers, notifications).

Do not merge them into “attachments.” Do not build either in HR-01.

### Challenge: official + pay on the card

Reference product put İŞKUR, SGK codes, AGİ/BES, education details, and payroll deductions on the same card. That is a **monolith card**. HuGuWeb shows those as **future tabs or module screens**, not as HR-01 fields on Genel Bilgiler.

---

## Create vs edit vs read

| Mode | Behavior |
|------|----------|
| Create | Same tabs that HR-01B implements. Hire still requires name, sicil, start date, department, position (Accepted). Identity/contact optional unless later required. Saving create remains one business operation: Employee + Employment + Primary Assignment **plus** profile if present. |
| Edit | Profile fields save as Personel Master update. Department/Position change remains **Transfer**, not a silent FK edit. Employment end remains **End Employment**. |
| Read | `hr.employee.read` without `hr.employee.manage`: no save. Highly sensitive fields omitted without `hr.employee.sensitive.read`. |

Do not offer “Personeli Sil” for a person who has (or had) employment.

---

## Çalışma tab vs existing commands

Do not re-implement Transfer / End Employment as row edits on Assignment history.

The Çalışma tab **displays** current employment and assignment and **invokes** existing commands. Personel Master must not invent a second status enum.

---

## Localization

UI strings: `tr` / `en` / `ru`.

Enums stored as English identifiers (`Tckn`, `Married`, `Active`). Labels translated.

National identity numbers, IBAN, personnel numbers, and official codes are **not** translated.
