# Organization Model

> **Status:** Accepted — Product Owner + CTO approved baseline. **Evidence:** E0–E1. Target segment is single-property independent mid-size hotels ([Target Customer](../../product/TARGET_CUSTOMER.md)).

## Organization ≠ Property

Persist both. Do not collapse them. Do not introduce Tenant or Hotel Group.

| Concept | Meaning | First implementation |
|---------|---------|----------------------|
| **Organization** | Employer / company boundary | Thin, required, one seeded instance. No tax, MERSİS, legal, or accounting master data. |
| **Property** | Physical operating hotel / tesis | Required, explicit, conceptually belongs to Organization. One instance. |
| **Hotel Group** | Portfolio / chain | Deferred. Not implemented. |
| **Tenant** | SaaS isolation | Not an HR concept. Not implemented. |

First implementation remains **single-property**. Architecture should simply avoid making future multi-property impossible. That means keeping Organization and Property as separate concepts — not building multi-property UI, tenant resolvers, or cross-property assignment workflows.

Seed **one Organization** and **one Property**. No Organization admin UI. No Property admin UI.

Employment belongs to the Organization (who employs you). Assignment is property-scoped through Department (where you work). For the first pilot those ids are constant.

### Rejected alternatives

| Alternative | Why rejected |
|-------------|--------------|
| Treat Organization = Property as one table | Makes later multi-property and personnel uniqueness ambiguous |
| Full legal-entity + group hierarchy now | Speculative; out of target segment |
| Skip Property | Contradicts accepted architecture (Property is explicit) |
| Skip Organization (building is the employer) | Encodes the wrong employer boundary |
| Tenant / Hotel Group now | Deferred product infrastructure |

---

## Department

Department is **customer-defined data**, not an application enum and not an i18n resource key.

Illustrative names only:

- İnsan Kaynakları
- Kat Hizmetleri
- Ön Büro
- Teknik Servis
- Yiyecek ve İçecek

Hotels rename, merge, and split these. Store one working name. Technical id is the stable identity.

### Attributes (conceptual)

| Attribute | Role |
|-----------|------|
| Technical id | Stable identity (not the name) |
| Optional code | Hotel-chosen short code if useful. Not a HuGuWeb enum. |
| Display name | Customer-defined working name |
| Property id | Departments are property-scoped in this model |
| Active / inactive | Inactive cannot receive **new** assignments |

### Hierarchy

**Flat list.** Do not add `ParentDepartmentId` yet. Architecture must not unnecessarily prevent hierarchy later.

A nested F&B tree is not required to hire, transfer, or end employment. If experts later show a real need, optional parent can be added without rewriting Employee.

### Lifecycle

Deactivate; do not hard-delete a department that has assignment history. Rename is allowed; identity stays the technical id. Historical assignments keep the department id.

---

## Position

Position is not Department, not a permission, not a Role, not a shift, and not a pay grade.

| | Department | Position |
|-|------------|----------|
| Example | Kat Hizmetleri | Kat Görevlisi |
| Question | Which organizational area? | Which job title in that area? |

Do **not** hard-code titles as enums. Do **not** add `IsManagerial` — it tends to become disguised authorization.

### Attributes (conceptual)

| Attribute | Role |
|-----------|------|
| Technical id | Stable identity |
| Optional code | Hotel-chosen if useful |
| Display name | Customer-defined working name |
| Department id | Position belongs to one department in this model |
| Active / inactive | Inactive cannot receive **new** assignments |

A position may *later* suggest a default permission bundle when creating a user. That mapping is authorization configuration, not a domain rule of Position.

First model: a position belongs to **one** department. Covering two departments is an Assignment concern (later Temporary), not a floating position.

---

## Localization of organization names

| Name | Category | Sprint 0.7B |
|------|----------|-------------|
| “Departmanlar” nav label | Product term | i18n `tr` / `en` / `ru` |
| Department name “Kat Hizmetleri” | Customer-defined data | Single stored string |
| Position name “Kat Görevlisi” | Customer-defined data | Single stored string |
| `Department` class / table | Domain identity | Never translated |

Do not implement customer master-data translation or a translation-management UI.
