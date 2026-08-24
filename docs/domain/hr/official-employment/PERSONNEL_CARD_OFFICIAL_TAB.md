# Personel Card — Resmî bilgiler

> **Status:** Accepted — HR-03A IA, with **Accepted Product Owner amendment — 2026-08-24** (composition tab). Not visual design copied from WebİK.

## Visible tab order (this slice)

When Resmî bilgiler is implemented, the **visible** Personel Card tabs are:

1. Genel bilgiler
2. Kimlik & iletişim
3. Çalışma / organizasyon
4. **Resmî bilgiler**
5. Geçmiş

This matches current HR-01B production tabs plus the official tab inserted before Geçmiş.

**Bildirge Kodları is not a top-level tab.** It is a **fieldset/section** inside Resmî bilgiler, same pattern as Kimlik / İletişim / Adres / Acil durum (`<fieldset>` + `<legend>` + `personnel.section*` i18n keys).

---

## Relationship to Accepted HR-01A card IA

[PERSONNEL_CARD.md](../personnel-master/PERSONNEL_CARD.md) (Accepted) froze a longer conceptual order including Ücret & Ödeme, Resmî Bilgiler, Belgeler, Evraklar, Geçmiş, and allowed tabs 4–7 to stay **hidden** until their slice.

HR-03A does **not** change HR-DOMAIN-002 Accepted status and does **not** rewrite that document.

| Conceptual HR-01A slot | Visible in HR-03B? |
|------------------------|--------------------|
| Ücret & Ödeme | Hidden (HR-01C / HR-09) |
| Resmî Bilgiler | **Shown** — this slice |
| Belgeler / Evraklar | Hidden (HR-04) |
| Geçmiş | Shown (already) |

Empty **submission** shells are **not** rendered. Do **not** add empty KBS / SGK submission sections. İŞKUR/BES/Sosyal/Eğitim on this tab are **master/configuration composition**, not government clients.

---

## Resmî bilgiler — sections

**Visual order (PO amendment 2026-08-24):**

1. Bildirge Kodları
2. İŞKUR Aylık İşgücü Çizelgesi
3. BES (Bireysel Emeklilik) — configuration only; **no AGİ**
4. Sosyal Bilgiler — **no** Anne kızlık soyadı; WebİK Vize → HuGu Çalışma İzni
5. Eğitim Bilgileri — reuse HR-01B Öğrenim Durumu; **no** organizational Bölüm

Compact fieldset/legend + existing HR grids. Tab may scroll internally. Do not enlarge the modal.

### A. Bildirge Kodları — **HR-03B**

Fields (edit with `hr.employee.manage`; read with `hr.employee.read`):

- **SGK İşyeri** — select an **existing** `SgkWorkplaceRegistration` for the Employment’s Property context. Do **not** type a workplace registration number here.
- Belge türü
- Tabi olduğu kanun
- Sigorta kolu
- SGK meslek kodu (searchable; display `CODE — NAME`; persist code)
- **Görev Kodu** (PO amendment; six HuGu codes)

Layout: existing two-column card grid, not a payroll dashboard.

Do **not** include on this section: SGK notified checkboxes, exit codes.

Workplace numbers are created in Property / organization configuration, not by repeating sicil entry on every card.

### B. SGK / Employment Classification — **not 03B**

EmploymentClassification (seasonal / retired / intern as a first-class type) waits for HR-02. Belge türü already carries some of that meaning in the reference; do not invent a second enum on this tab now. Overlap with HR-02 remains an expert question.

### C. KBS / government status summaries — **not 03B**

No payload chips in the header (already rejected in Personel Master). No KBS status list. No empty KBS / İŞKUR / SGK submission shells.

---

## Create flow

Yeni Personel does **not** become a government-registration wizard.

- Same HR-01B required hire fields.
- Resmî bilgiler tab may exist in create mode but all official fields stay optional.
- Saving create still means Employee + Employment + Primary Assignment + optional HR profile. Official profile is created only if the user filled codes; otherwise omitted.

No official field is HUGUWEB REQUIRED at hire. If a later expert freeze makes one field legally required to open Employment, document that exception before changing hire validation.

Personel Card save must **not** infer SGK submission completeness.

---

## Edit flow

```text
Personel list → Personel Card → Resmî bilgiler → Bildirge Kodları → Save
```

Save updates `OfficialEmploymentProfile` for the current (or selected historical) Employment. It does **not**:

- transfer department
- change Position
- end Employment
- submit SGK
- notify KBS
- create or edit Property SGK registrations (configuration surface)

SGK İşyeri on this section only **selects** an existing applicable registration.

Unsaved-changes guard (Accepted) includes these fields when the tab is implemented.

Geçmiş remains Employment/Assignment history. Official snapshot history is not a third timeline in 03B.

---

## Localization

New strings: `personnel.tabOfficial`, `personnel.sectionDeclarationCodes`, field labels, empty picker labels.

| TR | EN direction | RU direction |
|----|--------------|--------------|
| Resmî bilgiler | Official | Официальные данные |
| Bildirge Kodları | Declaration codes | Коды декларации |
| SGK İşyeri | SGK workplace | Рабочее место SGK |
| Belge türü | Document type | Тип документа |
| Tabi olduğu kanun | Applicable law | Применимый закон |
| Sigorta kolu | Insurance branch | Ветка страхования |
| SGK meslek kodu | SGK occupation code | Код профессии SGK |

Görev Kodu **is** a 03B UI string as of the PO amendment. Labels: [LOOKUP_CODES.md](LOOKUP_CODES.md).

Stored codes are never translated. Lookup display names for SGK numeric families stay in official Turkish in 03B (they are statutory text).

Enums/ids stay English. Keys in `tr.ts` / `en.ts` / `ru.ts` under `personnel.*`, grouped by experience, not by filename.
