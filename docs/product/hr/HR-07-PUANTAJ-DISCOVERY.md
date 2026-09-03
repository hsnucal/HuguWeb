# HR-07 — Puantaj Discovery

> **Status:** Accepted / Completed — Product Owner + CTO domain freeze (2026-09-03). HR-07A and HR-07B PO-accepted as the HR-07 MVP (2026-09-03).
>
> Domain and persistence model are **Accepted**. HR-07A (backend foundation) is **Completed**. HR-07B (monthly React grid + top-level sidebar) is **Completed / Accepted**. HR-07 overall is **Completed / Accepted** for the operational MVP.
>
> Deferred beyond this MVP (not implied to exist): PDKS/punch integration, period locking, official holiday engine, half-day/Partial cells, overtime/payroll/SGK.
>
> **Does not supersede** HR-DOMAIN-001, HR-DOMAIN-002, HR-DOMAIN-003, HR-04, HR-05A, HR-05B, or HR-06. Those remain **Accepted**.
>
> **WebİK** remains a capability reference only. It is not HuGu domain truth, architecture, naming, schema, or UI to copy. Snapshot files are **not** in this repository and must not be committed.
>
> Companion ADR: [ADR-011-Puantaj-Domain-Model.md](../../architecture/adr/ADR-011-Puantaj-Domain-Model.md) — **Accepted**.

---

## Slice identity

| | |
|--|--|
| Slice id | **HR-07** |
| EN | Attendance / Timesheet (operational monthly result) |
| TR | Puantaj |

Do **not** rename because older Personel Master planning used **HR-07** for Shift and **HR-08** for Attendance. Those Accepted texts are **not rewritten**. Current planning map: [README.md](README.md).

Accepted sibling freeze already states:

```text
HR-06 = PLAN
LeaveRecord = approved / HR-recorded ABSENCE FACT
HR-07 = ACTUAL / payroll-relevant attendance result
```

---

## 1. Product goal

Give hotel HR and department managers a **monthly operational answer** for each Employment on each Property-local date:

> What is **accepted** as this employee’s attendance result today — planned work, rest, approved leave, absence, or still unresolved?

Puantaj is **not** a second copy of the weekly roster. It is the **accepted result** surface that later overtime (HR-08) and payroll can consume.

Personnel Card remains employee-master / personnel-record oriented. Puantaj does not live inside the card as the primary workspace.

---

## 2. Why top-level module

Puantaj is a **daily operational control** used every month, often at month-close, by HR and department managers. It is not a personnel-file tab.

| Surface | Question it answers |
|---------|---------------------|
| Personel / Personnel Card | Who is this person? Employment, assignment, documents, leave balances |
| Vardiya Planlama | What was this person **planned** to work? |
| İzin Yönetimi | What leave was requested / approved? |
| **Puantaj** | What is **accepted** as the attendance result for payroll-relevant operations? |

Approved primary sidebar direction (implementation is **HR-07B**, not HR-07A):

```text
Ana Sayfa
Personel
Puantaj
Vardiya Planlama
İzin Yönetimi
...
```

Current code still nests leave and shift plan under Workforce subnav (`WorkforceLayout`). That is an IA debt to resolve when HR-07 is authorized — not a reason to hide Puantaj inside Personnel Card.

Top-level Puantaj navigation is **Accepted**. Shipping the sidebar item and monthly grid is **HR-07B**.

---

## 3. User personas

| Persona | Typical need | Access |
|---------|--------------|--------|
| Property HR / HR specialist | Close the month: see missing days, apply corrections, export operational totals | Property-wide `hr.attendance.read` + `hr.attendance.manage` |
| Department manager | Own department grid; correct obvious errors; cannot see other departments | AUTH-02 department scopes + attendance permissions |
| Corporate HR | Multi-property, but only after explicit Property context | Organization membership; **no silent default Property** |
| Employee | Out of HR-07 MVP | Self-service remains leave (`hr.leave.request`); no employee Puantaj edit |

Do **not** hardcode manager titles or emails. Roles are DB-managed permission bundles ([ADR-010](../../architecture/adr/ADR-010-Database-Managed-Authorization.md)).

---

## 4. Existing foundations HR-07 can reuse

Inspected against current domain (Accepted HR-00 … HR-06). Reuse means **read / authorize / display**, not mutate as Puantaj truth.

| Foundation | Reuse in HR-07 | Do not |
|------------|----------------|--------|
| `Employee` | Identity, name, personnel number | Store attendance on Employee |
| `Employment` | Row owner for the month; `StartDate`/`EndDate`/`Status` for InPeriod vs NotEmployed | Treat `EmploymentStatus` as attendance |
| `Assignment` + `EffectiveAssignmentResolver` | Department/Position on **that local date**; transfer-safe filter | Persist DepartmentId/PositionId on a Puantaj row as source of truth |
| `Department` / `Position` | Filter, sticky column, scope | Own the timesheet |
| `Property` + `TimeZoneId` | Explicit workplace; local calendar dates | Silent first/default Property |
| Active Property cookie `HuGuWeb.ActiveProperty` | Required operational context | Infer from membership list |
| `ShiftDefinition` | Planned code/name/times/net minutes | Copy semantic times onto Puantaj unless locking later snapshots |
| `ScheduleEntry` (`Shift` / `RestDay`) | **Planned source only** | Reuse as Puantaj result; overwrite to represent actual |
| `ScheduleEntryChange` | Pattern for append-only audit | Use schedule history as attendance history |
| Unscheduled = **no ScheduleEntry row** | Default Puantaj suggestion = Unresolved | Invent Unscheduled as a schedule kind |
| `LeaveType` | Display code/name/`SystemKind` | Match leave by localized name |
| `LeaveRecord` (`Recorded` only) | Approved/HR-entered absence fact | Use pending `LeaveRequest` |
| `LeaveRequest` | Out of grid truth; optional “pending” badge later | Drive accepted result |
| `LeaveAmount` 0.5-day quantum | Do not fake hourly cells | Invent AM/PM segments in HR-07 |
| `LeaveSchedulePreview` | How leave ranges meet Scheduled/RestDay/Unscheduled | Mutate `LeaveRecord.Amount` |
| `LeaveOverlap` | Two recorded leaves cannot share a date | Time-of-day segments (none exist) |
| `WorkType` | Later totals / part-time context | Day-status enum |
| AUTH-02 `UserMembershipDepartmentScope` | Department-aware attendance permissions | Frontend-only filtering |
| `IWorkforceClock` / `TimeProvider` | UTC timestamps | `DateTime.Now` |
| `EmployeeTenantGuard` / `WorkplaceGuard` | Tenant isolation | Cross-property leakage |
| Shift-plan grid UX (HR-06B) | Sticky employee column, department filter, tooltips, OutOfScope/NotEmployed presentation | Copy week-first interaction as the Puantaj primary |

Official holidays: **no HuGu calendar entity exists.** **Q6 Accepted:** deferred. Do not invent a Turkish holiday engine in HR-07.

---

## 5. WebİK confirmed findings

Reference snapshot: `C:\Users\hsnuc\Desktop\ik.webik.com.tr` (download meta `2026-08-24`). Analysis of `index.html`, `pdks_app.v_10969a00.js`, `sync.v_248e7f94.js` only. **No snapshot files were modified or imported.**

Frontend store/DTO names below are **UI evidence**, not a server schema to copy.

### A. Main Puantaj screen — CONFIRMED

TimeCore primary nav includes a distinct screen `puantaj` labeled **Aylık Puantaj**, sibling to **Shift Atama**, **Giriş / Çıkış**, and **Ana Ekran (Timecore)** — not inside Personel Kartı.

Component `AylikPuantaj` is a monthly employee × day grid.

### B. Date/month selection — CONFIRMED

Month picker (`YYYY-MM`), default = current month. **Listele** applies the month: loads that month’s `hareketler` window, then snapshots `shiftData` / izin / shifts. Changing the picker without Listele does not rebuild the snapshot.

### C. Department/person filtering — CONFIRMED

Department filter; admin sees all departments + **TÜMÜ**; non-admin limited to `kullanici.departmanlar`. Employees filtered by employment period intersection with the month. Row click selects a person (highlight). No dedicated free-text search control was found on this screen.

### D. Daily cell model — CONFIRMED

One cell per calendar day. Value is a **single code** from `pdks_shift_data` key `pid-YYYY-MM-DD`, falling back to `hareketler.shiftKod` if the shift store is empty for that key.

Display rules (comments in `getCellDisplay`):

| Store value | Puantaj display |
|-------------|-----------------|
| Working shift code (`CALISMA_KODLARI` = shifts that have `giris`) | Configurable `puantajCalismaYazisi` (default `"1"`) when a hareket exists; otherwise the **shift code, faded**, meaning “assigned, no punch yet” |
| Leave code / `OFF` / `RT` | The code itself |
| Empty | `·` (middle dot) on screen; print uses blank / `–` for inactive employment days |
| Pending approval | Hourglass |

Leave and rest are **the same cell** as the shift assignment. There is no separate attendance-result entity in the Puantaj grid.

### E. Shift display — CONFIRMED

Working days normally collapse to the firm parameter work mark (`"1"`), not the shift clock times. Colored mode (`puantaj_renkli`) paints shift definition colors. Tooltip can show `"<kod> vardiyası"`.

### F. Leave display — CONFIRMED

Leave types are codes in the **same** `pdks_shift_data` map as shifts. İzin tanımları (`getIzinler()`) supply those codes. Painting a leave code onto a day **is** how WebİK represents leave on Puantaj. HuGu already rejected this as domain truth in HR-05A / HR-06 (`LeaveRecord` independent of `ScheduleEntry`).

### G. Rest / off day — CONFIRMED

`OFF` is an explicit cell code (not “empty”). Empty ≠ OFF. Shift Atama enforces weekly OFF rules and “distance between OFFs”. HuGu already mapped this capability to `ScheduleEntryKind.RestDay`.

### H. Manual correction on Puantaj — CONFIRMED (limited)

Aylık Puantaj is primarily a **reader + totals/export** screen. It does **not** appear to write `pdks_shift_data` on cell click. Cell click selects the **row**. Right-click opens payroll export actions (Logo / Elektra), not a day editor.

Day assignment / overwrite happens on **Shift Atama** (and punches can fill empty shift keys from Giriş/Çıkış).

### I. Bulk operations — CONFIRMED on Shift Atama, not on Aylık Puantaj

Multi-cell assign, OFF, leave codes, RT, clear: Shift Atama. Puantaj bulk is print/Excel/payroll export, not grid mutation.

### J. Copy operations — CONFIRMED on Shift Atama

Previous-week copy exists on Shift Atama (skip leave codes; RT only onto official-holiday dates; 11-hour rest check). Not a Puantaj-screen operation.

### K. Approval states — CONFIRMED (shift/leave assignment approval, not period close)

`pdks_onay_bekleyen` with `durum === "bekliyor"` overlays hourglass on the Puantaj cell. This is assignment-request approval, not “month locked for payroll.”

### L. Overtime relationship — CONFIRMED as a sibling engine

Fazla mesai is computed from **hareket `mesai` fields** plus resmi tatil map, stored/consumed separately (`pdks_fm_*`, Bordro codes 05 / 11). Puantaj totals can show a Mesai column from hareketler. Hourly leave ledger explicitly does **not** rewrite `pdks_shift_data`.

### M. Hourly leave — CONFIRMED as a separate ledger

`pdks_saatlik_izinler` (`tur`: `mazeret` | `denklestirme`). Comments state: writing an hourly leave **as a day code** would paint the person as all-day leave and drain 7.5h FM. The hourly ledger **does not touch** vardiya / puantaj day cell / SGK day / yıllık izin.

### N. Official holiday — CONFIRMED as a firm calendar, not a hardcoded engine

`pdks_resmi_tatiller` (`{ tarih, ad, katsayi }`). Empty list ⇒ that date is a **normal** day. `RT` may be entered only on dates present in that list (Shift Atama). Working a shift on a tatil date splits NÇ vs ÇT using `katsayi`. Header/print can highlight weekend vs tatil.

### O. Absence handling — CONFIRMED as empty / missing, not a first-class Absent status

Missing assignment: empty cell + **BOŞ** count + banner “Shift Atama ekranından eksikleri tamamlayın.” Shift assigned but no hareket: faded shift code (“hareket girilmemiş”) — comments insist this is **not** treated as a program error. No dedicated `DEV` / Absent enum on the Puantaj cell.

### P. Reports — CONFIRMED

Print (department-grouped HTML), Excel, payroll file exports (Logo, Elektra) from context menu. Signature-list print variants. Totals persisted to `pdks_puantaj_tp_sgk` when Listele/render runs, for Bütçe-Gerçekleşen and Elektra.

### Q. Employee totals — CONFIRMED (UI column codes)

Reorderable summary columns default: **TP, NÇ, ÇT, HT, Tİ, TÜİ, SGK**, plus per-leave-code counts and a **BOŞ** column.

### R. Day/hour calculation rules — CONFIRMED (client-side)

- NÇ = normal worked days counted from **working shift codes that also have a hareket** (Puantaj grid path).
- ÇT = official-holiday excess (`katsayi - 1`) when a working shift+hareket lands on a tatil date.
- HT = count of `OFF`.
- Tİ = paid leave codes excluding yıllık; Yİ counted separately; TÜİ = unpaid leave codes.
- RT counted separately; comments say RT is not included in TP the same way.
- Monthly salaried: TP capped / forced toward **30** when the month is “fully worked”; daily-paid uses calendar days but SGK still capped at 30 (`_sgkGunTavan`).
- `puantajToplami30GunAlsin` firm parameter exists for 30-day month mode.
- Mesai hours from hareket `mesai` parsed as hour.minute (`2.30` = 2h30m), not decimal hours.

These are **WebİK payroll-day rules**, not HuGu MVP rules.

### S. Locked / finalized periods — CONFIRMED absent on Puantaj

A former **Tahakkuk** button on this screen was **removed** (comments: it computed nothing durable). Bordro has a separate “kilit” concept in permission comments. Aylık Puantaj has **no Open/Locked month state**.

### T. Audit / history — CONFIRMED absent for Puantaj cells

`pdksAudit` is used for personel card / transfer / zam — not for Puantaj cell edits. Shift store overwrite is last-write-wins in `localStorage` (with write-verify after quota bugs). `ScheduleEntryChange`-style history does not exist for TimeCore cells.

### Punches exist in the snapshot — CONFIRMED

`GET /api/hareketler` loads Personel Hareketleri (`gGiris`, `gCikis`, `shiftKod`, `mesai`, `tarih`, person `id`). Opening window is previous month + current month for scale. `_syncHareketlerToShiftData` copies `shiftKod` into empty `pdks_shift_data` keys only (`if (!sd[key])` — **does not overwrite** manual assignments). Kiosk/device live refresh is referenced in comments. Personnel card has `cihazNo`.

**Therefore:** WebİK Puantaj is **schedule-cell-centric with a punch overlay**, not a separate actual-attendance document. Actual entry/exit live on the Giriş/Çıkış screen and feed faded-vs-solid display plus FM — they do not become a distinct Puantaj row type.

---

## 6. WebİK inferred behavior

| Item | Inference | Why not CONFIRMED |
|------|-----------|-------------------|
| Server schema for shift/puantaj | `sync.js` mirrors `pdks_shift_data`, `pdks_puantaj_tp_sgk`, `pdks_saatlik_izinler` as tenant documents | Snapshot is the SPA + sync keys; no SQL/migrations in the folder |
| Puantaj “1” mark means “accepted worked day” | Display parameter, not a stored attendance status | Could be print convention only |
| Department permission `departmanPuantajYetkisi` gates Shift Atama writes | Used as `shiftGirmeYetkisi` | Exact server enforcement not in snapshot |
| Weekend greying is visual only | Print styles Sat/Sun grey; hotels still assign OFF explicitly | Not a domain weekend-off engine |
| Historical Puantaj changes if Shift Atama is edited later | Same `pdks_shift_data` key | No freeze/snapshot of accepted month |

---

## 7. WebİK not confirmed / limitations

| Topic | Status |
|-------|--------|
| True separate “actual attendance document” per day | **Not confirmed** — grid reads the shift map |
| Period close / lock on Puantaj | **Not confirmed** (Tahakkuk removed) |
| Cell-level audit (who changed OFF→Yİ) | **Not confirmed** |
| Half-day cell split on Puantaj | **Not confirmed** |
| GPS / live location | **Not confirmed** on this screen |
| Automatic SGK e-declaration from the grid | Totals feed SGK **day counts**; submission engine not evidenced here |
| HuGu-style Unscheduled vs RestDay vs Absent | WebİK empty vs OFF vs faded-shift is a **different** three-way split |

### Limitations HuGu must not copy

1. Leave **is** a painted schedule cell.  
2. Puantaj and Shift Atama share one mutable map — plan and “result” collapse.  
3. Punches fill empty plan cells (directionally useful) but the monthly grid is still schedule-shaped.  
4. No durable accepted-result audit.  
5. 30-day SGK/TP arithmetic and Elektra/Logo export are payroll products, not hotel operational MVP.  
6. localStorage-as-SoT and client-side SGK math.

---

## 8. HuGu product principles

Hotel-first, evidence before clone ([PRODUCT_PRINCIPLES.md](../PRODUCT_PRINCIPLES.md)):

1. **Plan ≠ actual.** `ScheduleEntry` stays plan. Puantaj never silently mutates it.  
2. **Leave ≠ schedule.** `LeaveRecord` stays the absence fact. Do not paint leave onto `ScheduleEntry`.  
3. **Empty ≠ rest ≠ absent.** Same lesson as HR-06.  
4. **Accepted result is explicit** once a human (or later a punch pipeline) asserts it. Derived suggestion is allowed; silent overwrite of sources is not.  
5. **Property-local calendar**, UTC technical timestamps.  
6. **Backend authoritative** department scope.  
7. **Calm dense SaaS grid** — brand accent `#862A51` / `color.brand.primary`, not WebİK teal clone.  
8. **MVP subset** of the long-term input list; leave the rest as typed boundaries.

Long-term inputs (not all in MVP):

1. planned shift  
2. approved leave  
3. actual entry/exit punches (future PDKS)  
4. manual HR corrections  
5. official holidays  
6. rest days  
7. overtime  
8. absence  
9. department / property context  

---

## 9. MVP scope

In:

- Top-level **Puantaj** workspace (route/nav when implementation is authorized)
- Explicit **Property** context
- **Month** in Property-local calendar
- **Department** filter (AUTH-02)
- Employee list for employments intersecting the month
- Per-day **accepted result** (read model) with planned shift **reference**
- Approved **`LeaveRecord`** integration (`Recorded` only)
- **RestDay** from schedule as default rest suggestion
- **Unresolved** when Unscheduled and no leave / no manual result
- **Manual correction** with required note + append-only audit
- Monthly **operational totals** (not SGK/payroll engine)
- Department-scoped access

Out unless PO later expands:

- Biometric / turnstile / GPS
- Payroll, wage, overtime **payment**
- Complex legal / 30-day SGK declaration engine
- Automatic SGK submission
- Microservices / event broker / Redis
- Employee self-service timesheet
- Bulk copy-week on the Puantaj screen (lives on Vardiya Planı)
- Official holiday engine
- Hourly / half-day cell physics
- Period lock UI (recommended deferred — §15)

---

## 10. Explicit non-goals

- Do not add `Leave` / `PublicHoliday` / `Unscheduled` kinds to `ScheduleEntry`.  
- Do not store remaining-balance or wage on Puantaj.  
- Do not implement `AttendancePunch` tables in HR-07 MVP.  
- Do not calculate overtime pay (HR-08).  
- Do not ship Elektra/Logo/WebİK export formats.  
- Do not put Puantaj CRUD on Personnel Card.  
- Do not treat pending `LeaveRequest` as accepted absence.

---

## 11. Main monthly workflow

```text
1. User has explicit active Property (cookie). TimeZoneId = that Property.
2. Open Puantaj → choose Month + Department (or all authorized departments).
3. Backend returns employments whose period intersects the month, scoped to
   Assignment.Department on each date (transfer-safe).
4. For each Employment × LocalDate in month:
     read ScheduleEntry, Recorded LeaveRecord covering the date,
     any AttendanceCorrection.
     compute AcceptedResult (precedence §12).
5. HR/manager scans Unresolved / Absent / conflicts.
6. Click cell → side panel: plan, leave, current result, correct, note, history.
7. Totals update from accepted results (derived; not a second payroll ledger).
```

Listele-style stale snapshot is **not** required if queries are date-bounded. Prefer fresh read per request; optional “as of” only if lock exists later.

---

## 12. Cell behavior

**Recommendation: single click → compact right-hand detail/edit panel** (drawer), not a large modal and not WebİK’s row-select + payroll context menu.

Rationale: operators compare adjacent days; a modal hides the grid. A popover is too small for plan + leave + history. A side panel keeps the grid visible.

Panel contents (MVP):

| Block | Source |
|-------|--------|
| Date (Property-local) + weekday | `DateOnly` |
| Employee + department/position on that date | Assignment covering date |
| Planned | ScheduleEntry + ShiftDefinition or RestDay or Unscheduled |
| Approved leave | Recorded LeaveRecord overlapping date (type, range, amount, note) |
| Current accepted result | Derived or correction |
| Manual correction | Status override + note (required on save) |
| Change history | `AttendanceCorrectionChange` (append-only) |

Keyboard: Esc closes. No silent save.

Display in the grid (labels, not cryptic-only):

| Accepted result | Compact cell | Tooltip |
|-----------------|--------------|---------|
| Worked from plan | Shift **code** or `08–17` from definition | Name, local start–end, net minutes |
| Leave | LeaveType **code** (e.g. Yİ) | Type name, record range, amount |
| RestDay | **OFF** | “Dinlenme günü (plan)” — not a shift code |
| Unresolved | **—** | “Sonuç yok / planlanmadı” |
| Absent (manual) | **DEV** or localized Absent | “Elle işaretlenen devamsızlık” |
| Manual override | Small marker | “Manuel düzeltme” + note excerpt |

Never show a code without tooltip/accessible name.

Sticky: employee column + header day row (`1 Sal`, `2 Çar`, …). Horizontal scroll for 28–31 days. Vertical virtualization only above a measured row threshold (WebİK learned 150; HuGu should pick after profiling — start with department-required filter so typical hotel pages stay small).

NotEmployed / OutOfScope cells: muted, not clickable for edit, same presentation idea as HR-06B.

---

## 13. Plan / leave / manual precedence

Deterministic, server-side. **No source is mutated** by applying a lower-priority fact.

### Frozen precedence (accepted result)

Highest wins. **Sources are never silently mutated.** Punch is a reserved slot only (no table/code in HR-07A).

| Priority | Fact | Accepted result | Source |
|----------|------|-----------------|--------|
| 1 | Active current `AttendanceCorrection` | Worked / Leave / RestDay / Absent as stored | **Manual** |
| 2 | Future punch observation | reserved — no code/table | **Punch** |
| 3 | `LeaveRecord` Status = Recorded covering LocalDate | **Leave** | **Leave** |
| 4 | `ScheduleEntry.Kind = RestDay` | **RestDay** | **Schedule** |
| 5 | `ScheduleEntry.Kind = Shift` | **Worked** (provisional; not observed attendance) | **Schedule** + `IsProvisional` |
| 6 | No schedule row | **Unresolved** | none |

A planned Shift alone is **not** evidence that the employee worked. HR-07 MVP has no PDKS, so Scheduled → Worked is **provisional**. Manual Worked is the same business status (`Worked`) with Source = Manual and `IsProvisional = false`.

`AcceptedWorkedMinutes` stays **null** in HR-07A. Schedule-derived Worked may expose `PlannedMinutes` from `ShiftLocalInterval`. Inventing actual minutes for Manual Worked is out of scope.

Pending `LeaveRequest` does **not** enter this table. Optional later: non-authoritative badge.

Cancelled `LeaveRecord` is ignored (row retained for leave audit, not for Puantaj).

### Manual correction (always wins accepted result)

| Actor action | Effect |
|--------------|--------|
| Set Worked / Leave / RestDay / Absent | Persisted override; plan and leave rows unchanged |
| Clear override | Reverts to suggestion from current sources |
| Note | Required |

### Future punch (not implemented; reserved)

| Priority | Rule |
|----------|------|
| Below manual | Punch is an **observation**. It may later change the *suggestion* (e.g. Scheduled+punch → still Worked, with actual minutes). |
| Must not overwrite | Manual correction, LeaveRecord, ScheduleEntry |
| Fill-empty only | WebİK `if (!sd[key])` is a warning: HuGu must **not** copy punches into `ScheduleEntry`. Punches write `AttendancePunch` later, never plan. |

### Overlay questions

| Situation | Accepted result | Overlay / HR-08 input |
|-----------|-----------------|------------------------|
| RestDay plan + manual Worked | **Worked** | `PlannedRestDay = true` → worked-on-rest (overtime boundary) |
| Official holiday + Worked (future calendar) | **Worked** | `OfficialHoliday = true` → worked-on-holiday, **not** status=Holiday |
| Official holiday + RestDay, no work | **RestDay** | Holiday flag only |
| Leave covering a RestDay | **Leave** (suggestion) | Plan remains RestDay; charged-day vs Amount is NEEDS VALIDATION |
| Scheduled + no punch (MVP, no PDKS) | **Worked** suggestion | Unresolved-for-punch is **not** MVP; do not fade every planned day as “missing clock” |

**Absence is not inferred from Unscheduled.** Hotels are 24/7. **Q3 Accepted:** Absent exists only via manual correction by a user with `hr.attendance.manage` inside authorized scope. Reason/note is mandatory.

---

## 14. Authorization

Follow existing permission catalogue style (`hr.leave.*`, `hr.schedule.*`).

| Code | Meaning | Department-aware? |
|------|---------|-------------------|
| `hr.attendance.read` | Open grid, see in-scope rows | **Yes** — Assignment department on that date ∩ allowed departments |
| `hr.attendance.manage` | Manual correction | **Yes** |
| `hr.attendance.close` | Lock/unlock month | **Yes** (Property-wide HR in practice); **do not seed in MVP** if lock is deferred |

`manage` implies `read` at policy evaluation (same pattern as schedule/leave UI helpers).

Scope:

| Membership | Effect |
|------------|--------|
| Property-wide (no department scope rows) | All departments in active Property |
| Property + department scopes | Only those departments |
| Organization-wide | Requires explicit Property cookie; still no silent default |

Backend authoritative. Unknown ids → 404 with stable codes. UI filters are UX only.

**Accepted seed (HR-07A, least privilege):**

- HR manager / specialist / corporate HR templates: `hr.attendance.read` + `hr.attendance.manage`
- `department-scheduler`: `hr.attendance.read` only (department-narrowed). Manage is **not** auto-granted; a department manager who should correct Puantaj receives `hr.attendance.manage` through an explicit role assignment.
- `hr.attendance.close` is catalogued for a future lock slice. It is **not** granted to HR or department templates. Development Superuser receives it only because that template is `PermissionCatalog.All`.

No hardcoded emails or job titles.

---

## 15. Multi-property / timezone / LocalDate

Puantaj is **Property-scoped**. Organization-wide users must select Property. No first/oldest/configured fallback ([TENANCY.md](../../architecture/foundation/TENANCY.md)).

| Concept | Type | Owner |
|---------|------|--------|
| Business day | `DateOnly` | Property-local calendar interpreted with `Property.TimeZoneId` |
| Month | year + month in that calendar | Same |
| Audit / created | `DateTimeOffset` UTC | `TimeProvider` |
| Future punch instant | `DateTimeOffset` UTC | Device/import; **map to LocalDate** via `TimeZoneId` at the Property |

Do **not** persist `TimeZoneId` on every Puantaj row. The Property is the timezone source (same as `ShiftDefinition`).

Overnight planned shifts: `ScheduleDate` remains the **start** local date (HR-06). Puantaj day D shows the shift that **starts** on D. Actual punch mapping across midnight is a PDKS concern, not an MVP cell split.

DST: TR hotels typically `Europe/Istanbul` (no DST since 2016). Do not over-engineer; keep IANA conversion in one application service.

---

## 16. Audit requirements

Business history owned by the attendance domain ([AUDIT.md](../../architecture/foundation/AUDIT.md) pattern B), analogous to `ScheduleEntryChange`:

On every successful correction (including clear):

- Previous accepted result (enum + leave/shift refs as they were)
- New accepted result
- Actor user id
- `ChangedAtUtc`
- Note / reason (required)
- EmploymentId + LocalDate
- PropertyId / OrganizationId as on other workforce audits

Do **not** replace this with a generic JSON audit table.  
Do **not** silently overwrite.

Leave cancel and schedule edits keep **their** histories. Puantaj history records **accepted-result** changes only.

---

## 17. Period finalization recommendation

**Q5 Accepted: do not implement Open/Locked in HR-07.**

| Question | Recommendation |
|----------|----------------|
| Can HR edit old months forever? | **Q4 Accepted:** yes in MVP, with audit. Backend does not prohibit past-month edits. UI warning is HR-07B. |
| Should payroll consume only locked periods? | There is no payroll consumer yet. |
| Lock now or later? | **Defer to HR-10 / payroll consumer.** Document the future states: `Open` / `Locked`. |

When lock exists:

- Locked month: `hr.attendance.manage` cannot correct; `hr.attendance.close` can unlock (policy NEEDS VALIDATION: dual control?)  
- Lock **materializes** accepted results so later plan/leave edits cannot rewrite history  
- Until then, open-month derivation is a feature (approve leave yesterday → today’s Puantaj updates)

WebİK evidence: no Puantaj lock; do not copy Tahakkuk theatre.

---

## 18. Future PDKS boundary

Conceptual only — **no tables, no devices**:

```text
AttendancePunch
  EmploymentId
  TimestampUtc          # DateTimeOffset
  SourceSystem          # e.g. Device, Import, ManualPunch
  DeviceId?             # opaque
  Direction?            # In / Out / Unknown  — NEEDS VALIDATION
```

HR-07 MVP must not block this:

- Accepted result `Source` is an explicit enum: `Schedule`, `Leave`, `Manual`, later `Punch`.  
- Do not write punches into `ScheduleEntry`.  
- Do not assume one punch pair per day in the day model (hotels / split shifts later).  
- Day LocalDate is computed from Property TZ, not from browser TZ.  
- Correction table stays valid when punches arrive: manual still wins.

---

## 19. Future overtime / payroll boundary

HR-07 exposes **facts**, not pay:

| Fact | MVP |
|------|-----|
| Planned net minutes | From `ShiftLocalInterval` when plan is Shift |
| Accepted result kind | Worked / Leave / RestDay / Absent / Unresolved |
| Leave minutes | Not in MVP (day-level only); Amount stays on LeaveRecord |
| RestDay flag (plan) | Yes |
| Official holiday flag | No (no calendar) |
| Actual accepted minutes | Null in MVP unless manual hours added (not recommended now) |
| Worked-on-rest | Manual Worked + plan RestDay |

HR-08 may compute overtime minutes/pay from punches + these flags.  
Payroll/SGK 30-day capping is **out**. Do not persist TP/SGK numbers in HR-07.

---

## 20. Leave integration

| Rule | Detail |
|------|--------|
| Truth | `LeaveRecord` with `Status = Recorded` |
| Ignore | `LeaveRequest` Pending/Rejected/Cancelled; `LeaveRecord` Cancelled |
| Full-day overlay | Inclusive `StartDate`..`EndDate` ∩ month, for **display and default accepted Leave** |
| Amount | Remains authoritative on the leave domain; Puantaj must **not** recompute or store remaining balance |
| Half-day | Domain quantum exists; **no AM/PM or clock fields**. MVP does **not** split a cell. If Amount is 0.5 on a 1-day range, show Leave suggestion + tooltip “0.5 gün — hücre bölünmez” (NEEDS VALIDATION on exact UX) |
| Hourly leave | **Not in domain.** Do not fake WebİK `pdks_saatlik_izinler` |
| Overlap with schedule | Leave suggestion overrides Shift/RestDay **result**; schedule row unchanged |
| Cancelled leave | Drops out of suggestion immediately (open month) |
| Two recorded leaves same day | Impossible (`LeaveOverlap`) |

**Q1 Accepted:** paint every calendar date covered by a Recorded `LeaveRecord` as Leave. Leave Amount/entitlement accounting remains Leave domain responsibility. Puantaj does not recompute leave balance. If Amount ≠ calendar day count, all covered dates still resolve Leave.

---

## 21. Shift integration

| Schedule state | Default Puantaj suggestion |
|----------------|----------------------------|
| Shift | Worked (show definition code/times) |
| RestDay | RestDay (show OFF) |
| Unscheduled (no row) | Unresolved (show —) |

NotEmployed (outside Employment period): no suggestion, disabled cell — presentation only.

Do not conflate `GetScheduleState` with accepted result. Schedule APIs stay as they are; Puantaj **reads** them.

---

## 22. Official holidays

**No HuGu official-holiday entity.** Tests mentioning `PublicHoliday` are schedule-preview placeholders, not a calendar product.

HR-07: **do not** build a Turkish gazetted-holiday engine.

Future boundary (NEEDS VALIDATION): Property- or Organization-scoped `OfficialHoliday` `{ LocalDate, Name }` — overlay flag only, never a mutually exclusive day status.

Until then, holiday+worked cannot be represented except via note on a manual correction.

---

## 23. Data model options

Conceptual read model (every Employment × LocalDate in the visible month):

```text
AttendanceDay (read)
  EmploymentId
  LocalDate
  AssignmentId                 # resolved at read (covering date)
  Plan: Unscheduled | RestDay | Shift(+ShiftDefinitionId)
  LeaveRecordId?
  AcceptedResult               # enum, not a free string
  IsManualOverride
  Source                       # Schedule | Leave | Manual | (future Punch)
  PlannedNetMinutes?           # derived from definition
  Notes?                       # from correction
```

### Option A — persist one AttendanceDay row per Employment per date

Pros: simple queries; ready for lock snapshots; punch minutes have a home.  
Cons: generate/repair rows when opening a month; stale copies of plan/leave; 31 × employees writes; recalc bugs when sources change.

Scale: 100 hotels × 80 employees × 31 ≈ 248k rows/month — **Postgres-fine**, not the reason to reject A. Complexity and stale-derived-data **are**.

### Option B — derive days; persist only corrections (and later punches)

Pros: plan/leave remain SoT; open-month recalc is free; sparse writes; matches “do not store derived data unnecessarily.”  
Cons: past-month historical drift if HR edits last month’s roster; lock still needs materialization later; list query is a join (schedule + leave + corrections).

### CTO recommendation

**Option B for MVP persistence. AttendanceDay is the read/application model, not a required table on day one.**

Persist:

- `AttendanceCorrection` (unique EmploymentId + LocalDate)  
- `AttendanceCorrectionChange` (append-only)

Derive the rest. When period lock ships, **then** materialize Option A rows for locked months only.

AssignmentId: resolve at read (like schedule writes pin it). Do not copy ShiftDefinition times onto the correction. Do not persist WorkedMinutes if they only echo planned net.

Challenged fields from the brief:

| Field | Verdict |
|-------|---------|
| EmploymentId | Persist on correction; always in read model |
| AssignmentId | Derived at read; pin on correction **at write time** for transfer-safe audit (same as ScheduleEntry) |
| LocalDate | `DateOnly` — persist on correction |
| Status | AcceptedResult enum — persist only if override |
| ScheduledShiftId | Derived from ScheduleEntry |
| ActualStart/End | **Defer** (punches) |
| LeaveRecordId | Derived |
| WorkedMinutes / AbsenceMinutes | Derive; don’t persist in MVP |
| ManualOverride / Source / Notes | Persist on correction |
| Created/Updated | On correction; history table for before/after |

---

## 24. Day-state model

Avoid one giant mutually exclusive enum that includes Holiday, Partial, and Worked.

```text
BASE COVERAGE
  InPeriod | NotEmployed

PLAN (from ScheduleEntry)
  Unscheduled | RestDay | Scheduled

ABSENCE FACT (from LeaveRecord)
  none | LeaveCovering

ACCEPTED RESULT (Puantaj)
  Unresolved | Worked | RestDay | Leave | Absent

OVERLAYS (flags, not competing statuses)
  ManualOverride
  PlannedRestDay          # even if accepted Worked
  OfficialHoliday         # future
  (future) PunchObserved
```

Mutually exclusive: **Accepted result** values.  
Not exclusive: holiday vs worked; rest-plan vs worked-result.

Do **not** freeze UI words as domain names without PO OK. Proposed English enum identifiers:

`Unresolved | Worked | RestDay | Leave | Absent`

TR display: Planlanmadı / Çalıştı / OFF / (leave code) / Devamsız.

`Partial` is **deferred** (hourly / half-day physics).

---

## 25. Reporting (MVP totals)

Per employee for the month, from **accepted results** (not WebİK TP/SGK):

- Planned work days (Schedule Shift count)  
- Accepted Worked days  
- Leave days (accepted Leave cells)  
- Rest days (accepted RestDay)  
- Absence days (accepted Absent)  
- Unresolved / missing days  
- Planned net minutes sum for Worked-from-plan cells  

Footer: optional per-day Worked headcount (operational), not a legal report suite.

---

## 26. Scale / query shape

Assumptions: 100+ properties, thousands of employees, one month grid.

| Tactic | Choice |
|--------|--------|
| Filter | **Require** department (or “all authorized”) server-side; do not download the whole property if scoped |
| Query | One month `DateOnly` range: ScheduleEntry, LeaveRecord overlapping range, Corrections for range, Employments intersecting range |
| Indexes (when implemented) | `(EmploymentId, ScheduleDate)` already on schedule; leave `(EmploymentId, StartDate, EndDate)`; correction unique `(EmploymentId, LocalDate)` + `(Property via assignment/department)` |
| Pagination | Department pages; virtualize rows if > ~150 (measure) |
| Cache | No Redis. No extra microservice |

---

## 27. Grid UX (HuGu, not WebİK)

- Route proposal (later): `/app/workforce/puantaj` or top-level `/app/puantaj` once sidebar freeze is implemented  
- Header: Property (existing shell), Month, Department, Search name/sicil, optional result-status filter  
- Accent `#862A51`; calm density; sticky name + days  
- Accessible tooltips (`title` + later proper tooltip)  
- Do not clone WebİK print/SGK column soup in MVP (TP/ÇT/SGK)

---

## 28. PO decisions (Accepted 2026-09-03)

| ID | Decision |
|----|----------|
| **Q1 Leave paint** | Calendar dates covered by an approved/recorded `LeaveRecord` resolve as **Leave**. Amount/entitlement accounting remains Leave domain. Puantaj does not recompute leave balance. |
| **Q2 Half-day** | **Deferred.** Do not fake Partial support. No AM/PM cell split. |
| **Q3 Absent** | May be set only through manual attendance correction by a user with `hr.attendance.manage` inside authorized scope. Reason/note is **mandatory**. Absent is never inferred. |
| **Q4 Past month** | Editable in MVP with audit. UI warning is HR-07B. Backend does not prohibit past-month edits. |
| **Q5 Period lock** | **Deferred** until payroll/tahakkuk consumer. No Open/Locked table in HR-07A. |
| **Q6 Official holiday** | **Deferred.** No Turkish holiday engine in HR-07A. |
| **Q7 Punch direction** | **Deferred.** |
| **Q8 Unlock** | **Deferred** with lock. |
| **Q9 Sidebar** | Top-level Puantaj navigation **approved**. Implementation is **HR-07B**. |
| **Q10 Cell text** | Future UI displays scheduled time e.g. `08:00–17:00`; shift code is secondary/detail only. Not implemented in HR-07A (no grid). |

---

## 29. Docs / ADR

| Doc | Status |
|-----|--------|
| This file | **Accepted / Completed** (HR-07 MVP) |
| [ADR-011](../../architecture/adr/ADR-011-Puantaj-Domain-Model.md) | **Accepted** |
| HR-07A backend foundation | **Completed** (2026-09-03) |
| HR-07B monthly grid + sidebar | **Completed / Accepted** (2026-09-03) |

---

## 30. HR-07A implementation (backend foundation)

> **Status:** Completed (2026-09-03). Backend foundation verified (build, tests, manual API).

HR-07A ships the domain, sparse persistence, resolver, monthly query API, correction/history APIs, authorization, and tests. It does **not** ship a React grid or top-level sidebar.

### Critical domain freeze

```text
ScheduleEntry = PLAN
LeaveRecord   = APPROVED / RECORDED ABSENCE FACT
Puantaj       = ACCEPTED / RESOLVED ATTENDANCE RESULT
```

Puantaj never mutates `ScheduleEntry`. A `LeaveRequest` is never stored as an attendance fact. Only `LeaveRecord` with `Status = Recorded` participates. Correction `Leave` is an accepted attendance override, not a new leave entitlement / `LeaveRecord`.

### Persisted correction-only model (Option B)

No `AttendanceDay` table. No monthly snapshot. No period/lock table. No punch table.

| Table | Role |
|-------|------|
| `AttendanceCorrections` | Current-state override, at most one row per `(EmploymentId, LocalDate)` (unique index) |
| `AttendanceCorrectionChanges` | Append-only audit of Set and Clear |

Clear **deletes** the current override row so resolution falls back to Schedule/Leave. History rows are retained (`CorrectionId` is nullable after clear).

### Resolver precedence

Implemented in `AttendanceDayResolver`:

1. Active current `AttendanceCorrection` → accepted kind from correction, `Source = Manual`, `IsProvisional = false`
2. Punch — enum reserved (`AttendanceSource.Punch`); no table or resolution code
3. Recorded `LeaveRecord` covering the Property-local date → `Leave` / `Source = Leave`
4. `ScheduleEntry` RestDay → `RestDay` / `Source = Schedule`
5. `ScheduleEntry` Shift → **provisional** `Worked` / `Source = Schedule` / `IsProvisional = true`
6. Else → `Unresolved` (`IsUnresolved = true`, no source)

Absent is **never** inferred. Dates outside employment coverage are `NotEmployed` (not Unresolved, not Absent). `OutOfScope` is presentation/security only (wrong property or department filter).

### Provisional Worked

A planned Shift is not observed attendance. The accepted kind is still `Worked`. Provenance distinguishes:

- Schedule-derived: `Source = Schedule`, `IsProvisional = true`, `AcceptedWorkedMinutes = null`, `PlannedMinutes` from `ShiftLocalInterval` when the definition exists
- Manual Worked: `Source = Manual`, `IsProvisional = false`, `AcceptedWorkedMinutes = null` (no punch times; minutes are out of scope for MVP)

### API contracts

Property comes from explicit active workplace context (`HuGuWeb.ActiveProperty`). OrganizationId/PropertyId are not accepted as query parameters.

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/api/hr/attendance/monthly?year=&month=&departmentId=&search=` | `hr.attendance.read` (manage also satisfies read) |
| GET | `/api/hr/attendance/{employmentId}/{date}/history` | `hr.attendance.read` |
| PUT | `/api/hr/attendance/{employmentId}/{date}/correction` body `{ kind, reason }` | `hr.attendance.manage` |
| DELETE | `/api/hr/attendance/{employmentId}/{date}/correction` | `hr.attendance.manage` |

Monthly response: month metadata, filter departments, employees overlapping the month, per-day `AttendanceDayResult`, operational totals (`WorkedDays`, `LeaveDays`, `RestDays`, `AbsentDays`, `UnresolvedDays`, `PlannedMinutes`). No payroll/SGK/overtime totals.

### Audit

Every successful Set (create or change) and Clear writes `AttendanceCorrectionChange` with previous/new kind+reason, actor user id, and `TimeProvider` UTC. Identical Set is a no-op (no extra history).

### Authorization

| Code | Catalog | HR templates | `department-scheduler` |
|------|---------|--------------|------------------------|
| `hr.attendance.read` | yes | yes | yes (least privilege; AUTH-02 department-narrowed) |
| `hr.attendance.manage` | yes | yes | **no** |
| `hr.attendance.close` | yes (future lock) | **no** | **no** |

Development Superuser receives `close` only via `PermissionCatalog.All`. Backend enforces department scope from `MembershipDepartmentAccess`; the client cannot widen scope with `departmentId`.

Frontend product UI is untouched. Authorization i18n labels (`tr`/`en`/`ru`) were added because the permission catalog is shown on the existing Roles admin screen.

### LocalDate

Correction `LocalDate` is Property-local `DateOnly` (`date` column). Audit timestamps are UTC `DateTimeOffset`. Timezone is not copied onto correction rows. `Property.TimeZoneId` remains canonical. No silent default Property.

### Query strategy

One bounded batch per month: overlapping employments, assignments, month `ScheduleEntry` rows, overlapping Recorded `LeaveRecord`s, month corrections, shift definitions, leave types. Resolve in memory. No per-day EF round trips. No Redis.

### Stable error codes (kebab-case, project convention)

`attendance-invalid-month`, `attendance-outside-employment`, `attendance-correction-reason-required`, `attendance-correction-reason-too-long`, `attendance-correction-kind-invalid`, `attendance-employment-not-found`, `attendance-department-scope-denied`, `attendance-property-access-denied`, `attendance-assignment-not-found`, `attendance-department-filter-denied`.

Wrong organization is `attendance-employment-not-found` (no tenant existence leak). TR/EN/RU `HrMessages` resources localize title/detail.

### Migration

Workforce: `20260902201656_AddAttendanceFoundationHr07A` — `AttendanceCorrections` + `AttendanceCorrectionChanges` only.

### Known deferrals (after HR-07 MVP)

These capabilities are **not** in the accepted MVP. Do not imply they exist:

- PDKS / punch integration (`AttendancePunch`)
- period locking / `hr.attendance.close` behavior
- official holiday engine
- half-day / Partial cells
- overtime / payroll / SGK

---

## 31. HR-07B implementation (monthly workspace)

> **Status:** Completed / Accepted (2026-09-03). Product Owner manual retest passed.

HR-07B ships the top-level Puantaj sidebar, monthly operational grid, overlay correction/detail drawer, history, and department-scoped reads/writes. It does **not** add an `AttendanceDay` table, mutate `ScheduleEntry` or `LeaveRecord`, or introduce an HR-07B migration.

### PO findings (resolved)

| Finding | Resolution |
|---------|------------|
| Internal catalog code `annual` leaked into Puantaj cells/detail | System-known leave types localize via `LeaveType.SystemKind` / known codes. Cells use compact labels (TR `Yİ`, EN `AL`, RU `ОТП`). Detail uses full names (TR `Yıllık İzin`). Custom tenant types keep configured name/code. |
| Manual correction failed for seeded/existing Personnel (Hasan Uçal succeeded) | Correction PUT requires **EmploymentId**. Using EmployeeId returns `404` `attendance-employment-not-found`. UI writes the day employment id. Dated assignment validation is unchanged. `EmployeeAccountLink` is not required. Department/property authorization was not weakened. |
| In-flow side panel shrank the 31-column grid | Detail/correction UI is a right overlay drawer. The grid keeps full workspace width; scroll position is preserved. |

---

## Related

- [HR-06-Shift-Work-Schedule.md](HR-06-Shift-Work-Schedule.md)  
- [HR-05A-Leave-Foundation.md](HR-05A-Leave-Foundation.md)  
- [HR-05B-Leave-Request-Approval.md](HR-05B-Leave-Request-Approval.md)  
- [TIME_AND_TIMEZONE.md](../../architecture/foundation/TIME_AND_TIMEZONE.md)  
- [DEPARTMENT_MEMBERSHIP_SCOPE.md](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md)  
