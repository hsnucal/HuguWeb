# Import, export, and bulk photo

> **Status:** Accepted — HR-01A. Conceptual workflows and rules. **HR-01C** to implement. Do not add a spreadsheet library in HR-01A or HR-01B.

## Excel export (rules freeze)

- Only fields the user is **authorized to see** may export.
- Highly sensitive columns require `hr.employee.sensitive.read` (and must not be on the default export).
- Current list filters should **optionally** apply to export (export-what-I-am-looking-at).
- Derived / system-controlled values (tenure, leave balance, payroll result, performance score, effective employment state computed from dates) must be labeled as derived if ever exported later — they are **not** Personel Master import sources.
- Audit of exports is later.

Default export = default list columns (normal HR data). Column picker may add eligible columns the user may see.

---

## Excel import (rules freeze)

Persisted **input** data may be imported. Derived/system-controlled data must not be blindly imported.

### Import candidates (when authorized)

PersonnelNumber, GivenName, FamilyName, email, phone, birth date, department **reference**, position **reference**, optional identity/contact fields per catalog.

Department and Position import by **stable id or unique working code**, not by hoping the display name is unique. Position names are not unique in the Accepted model.

### Not direct imports

Calculated tenure, leave balance, payroll result, performance score, derived employment state, GUIDs of other aggregates, SGK submission flags, cumulative tax.

National identity numbers: import only with sensitive permission; never as the Employee primary key.

### Flow

```text
Template → Upload → Column mapping → Validation preview
  → errors / warnings → Confirm → transactional or batched import
  → result report
```

Never silently partial-import without a result report.

**REFERENCE PRODUCT OBSERVATION:** WebİK matched existing rows primarily by TCKN, auto-created sicil, and treated unmatched TC as insert. HuGuWeb primary match is **PersonnelNumber** within Organization. TCKN match is an optional later fallback for authorized HR only — it must not become the public matching story in the UI.

Create-via-import still must satisfy Hire invariants (employment + primary assignment) or be rejected. Do not import “orphan employees” without employment unless a later staging concept exists (it does not in HR-01).

---

## Bulk photo (HR-01C)

```text
ZIP / files → filename match → preview
  → unmatched / duplicate / invalid → confirm → result
```

| Rule | Choice |
|------|--------|
| Primary match | PersonnelNumber (filename stem) |
| Optional fallback | National identity number, only if explicitly allowed and user has sensitive permission |
| TCKN in UX | Do not show unmatched TCKN filenames to users without `hr.employee.sensitive.read` |
| Replace | Confirm; last confirmed wins per employee |
| Types / size | Image types only; size cap at implementation |
| Close during upload | Do not allow dismiss that leaves a silent half-batch |

**REFERENCE PRODUCT OBSERVATION:** WebİK bulk photo matched “TC or sicil” in the filename. HuGuWeb prefers sicil so TCKN does not leak through Finder/Explorer.

---

## Toplu Zam

Present in the reference product as a Personel-list bulk action.

**HuGuWeb:** Compensation / **HR-09**. Not Personel Master. Not HR-01B. Not HR-01C.
