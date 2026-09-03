# HR product planning index

> **Status:** Planning index only. Does **not** change Accepted HR-DOMAIN-001, HR-DOMAIN-002, or HR-DOMAIN-003.

Slice identifiers are product/planning labels. Older Accepted Personel Master text that used different numbers remains historically valid and is **not** rewritten here.

| Current slice id | Meaning | Older planning alias (Accepted Personel Master maps) |
|------------------|---------|--------------------------------------------------------|
| HR-00 | Organization & Workforce Foundation | unchanged |
| HR-01 | Personel Master | unchanged |
| HR-03 | Official employment / Bildirge Kodları | unchanged |
| **HR-04** | **Employment & Working Conditions / Çalışma Bilgileri** | sketched as **HR-02** entry/exit in Personel Master planning |
| Documents (Belgeler / Evraklar) | Later personnel-file slice — **not** this HR-04 | sketched as **HR-04** in Personel Master planning |
| Temporary assignment / promotion | Later Assignment UI | sketched as **HR-05** in Personel Master planning |
| **HR-05A** | **Leave Management Foundation / İzin Yönetimi Temeli** | sketched inside **HR-06–08** leave / shift / puantaj in Personel Master planning |
| **HR-05B** | **Leave Request & Approval** — Accepted / Completed | same older leave bucket |
| **HR-06** | **Shift & Work Schedule / Vardiya & Çalışma Planı** — Accepted / Completed | sketched inside **HR-06–08** leave / shift / puantaj in Personel Master planning |
| **HR-06A** | **Shift & Schedule Foundation** — Accepted / Completed | part of HR-06 |
| **HR-06B** | **Weekly Shift Planning** — Accepted / Completed | part of HR-06 |
| **HR-07** | **Puantaj / Attendance (operational monthly result)** — **Accepted**. HR-07A backend foundation implemented; HR-07B grid/sidebar not started | sketched as **HR-08** Attendance in older Personel Master planning; later Accepted texts already call future actuals **HR-07** |
| **Personnel Enrichment + Onboarding Documents** | Certificates, WorkType, Probation, Recruitment Source, Onboarding checklist + printable templates; Department-scheduler seed fix | — |

Current Accepted freeze: [HR-04-Employment-Working-Conditions.md](HR-04-Employment-Working-Conditions.md) — Domain Frozen decisions remain **Accepted**. Implementation is **Accepted**.

Current Accepted freeze: [HR-05A-Leave-Foundation.md](HR-05A-Leave-Foundation.md) — domain **Accepted**. Does not supersede HR-04 or HR-DOMAIN-001/002/003.

Implementation: [HR-05A-Leave-Implementation-Plan.md](HR-05A-Leave-Implementation-Plan.md) — **Accepted / Completed**. Domain freeze remains Accepted. Product Owner manual acceptance completed (2026-08-29).

Leave request domain: [HR-05B-Leave-Request-Approval.md](HR-05B-Leave-Request-Approval.md) — domain freeze **Accepted / Frozen** (2026-08-31). Plan: [HR-05B-Leave-Request-Approval-Implementation-Plan.md](HR-05B-Leave-Request-Approval-Implementation-Plan.md) — **Accepted / Completed**. Product Owner runtime acceptance completed (2026-08-31). Personnel Card Talepler remains deferred as a follow-up surface, not a blocker for HR-05B completion.

Current Accepted freeze: [HR-06-Shift-Work-Schedule.md](HR-06-Shift-Work-Schedule.md) — domain **Accepted**. Does not supersede HR-05A or earlier HR domains. WebİK remains reference only. Product Owner acceptance completed (2026-08-30): HR-06 overall **Accepted / Completed**.

Implementation plan: [HR-06A-Shift-Schedule-Implementation-Plan.md](HR-06A-Shift-Schedule-Implementation-Plan.md) — **Accepted / Completed** (foundation). AUTH-02 department scopes included.

Weekly planning: [HR-06B-Weekly-Shift-Planning.md](HR-06B-Weekly-Shift-Planning.md) — **Accepted / Completed**.

Personnel enrichment: [HR-PERSONNEL-ENRICHMENT-ONBOARDING.md](HR-PERSONNEL-ENRICHMENT-ONBOARDING.md) — **Implemented / Awaiting PO Acceptance**. Includes Department manager schedule permission seed fix (`department-scheduler`) and Çalışma Bilgileri nested submenu IA (single edit session; no extra APIs).

Department authorization: [DEPARTMENT_MEMBERSHIP_SCOPE.md](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md) — **AUTH-02 Accepted / Completed**.

HR-07 discovery: [HR-07-PUANTAJ-DISCOVERY.md](HR-07-PUANTAJ-DISCOVERY.md) — **Accepted** (2026-09-03). Companion: [ADR-011](../../architecture/adr/ADR-011-Puantaj-Domain-Model.md) — **Accepted**. HR-07A backend foundation is implemented (corrections-only persistence, resolver, monthly/correction/history APIs). HR-07 overall remains **In Progress** until HR-07B (monthly grid + top-level sidebar). WebİK remains reference only. Puantaj is a top-level operational module, not a Personnel Card tab.
