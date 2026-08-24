# Official Employment field catalog

> **Status:** Accepted — HR-03A. Catalog only. **Not** schema, EF, or API contracts.
>
> **Requiredness class:** `REFERENCE REQUIRED` = required in the WebİK reference for a specific downstream action (not necessarily personel save) · `HUGUWEB REQUIRED` = required to persist in our accepted business model · `OPTIONAL` = may be empty after hire · `NEEDS LEGAL/EXPERT VALIDATION` = do not freeze as mandatory from UI asterisks (none were present on Bildirge Kodları).
>
> Source/reference notes are **REFERENCE PRODUCT BEHAVIOR** unless marked HuGuWeb decision.

---

## Property-level — workplace registrations

A Property **may** have **0..\*** `SgkWorkplaceRegistration` records. There is **no** “one active registration per Property” invariant. Do **not** invent the complete legal/accounting field set here.

| Product label (TR) | Technical | Type | Owner | Requiredness | Lookup/free-text | Effective-dated? | Sensitivity | Personel Card section | Future SGK | Future KBS | Excel later? | Validation notes | Source / evidence |
|--------------------|-----------|------|-------|--------------|------------------|------------------|-------------|-----------------------|------------|------------|--------------|------------------|-------------------|
| SGK işyeri sicil no | RegistrationNumber | string | `SgkWorkplaceRegistration` / Property | OPTIONAL in 03B configuration; NEEDS LEGAL/EXPERT VALIDATION before SGK submit | Free-text with format check **only if** digits-only length is treated as a hint (source: ≥21 digits to parse components). Do not invent a legal regex. | Optional ValidFrom/ValidTo on the registration row later; not required to invent now | Organization confidential | **Not typed on Personel Card.** Created in Property/organization configuration. Card **selects** an existing registration. | Header of işe giriş / hizmet / işyeri context | No | Restricted later admin import | Store digits; display may group. Do not duplicate the number onto Employment. | Ref Firma → Çalışma → “Sicil No” (`sgkSicilNo`). Parsed into mahiyet/işkolu/ünite/sıra/il/ilçe/CD/aracı. |
| (system) | PropertyId | Guid | `SgkWorkplaceRegistration` | HUGUWEB REQUIRED | FK | — | Normal | Not on card | Scopes workplace | — | No | Must equal the Property that owns the registration | HuGuWeb |
| (system) | Id | Guid | `SgkWorkplaceRegistration` | HUGUWEB REQUIRED | — | — | Normal | Not on card | Referenced by profile | — | No | Each registration is a separate business record | HuGuWeb |
| İşkolu (ÇSGB) | WorkplaceBranchCode | string | `SgkWorkplaceRegistration` optional later | OPTIONAL | Lookup if kept (source 01–20; hotel typically 18 Konaklama) | No | Organization confidential | Not on card | May appear on paper bildirgeler | No | No | Prefer parse-from-sicil later; do not duplicate unless UI needs it. **Out of 03B minimum.** | Ref Firma “İş Kolu” |
| SGK ünite | SgkUnitName / code | string | later | OPTIONAL | Free-text | No | Organization confidential | Not on card | Possible payload | No | No | **Out of 03B minimum.** Parse from sicil when needed. | Ref `sgkUnitesi`, `sgkUniteKodu` |
| VKN / vergi no | TaxNumber | string | **Not Property in this slice.** Likely future Organization legal profile | Out | — | — | HighlySensitive org | Not on card | Employer header later | Later | No | Do not expand Property into a legal master. Expert: Organization vs out. | Ref Firma “Vergi Kimlik No (VKN)” — company, not employee |
| SGK kullanıcı / şifre / işyeri kodu | credentials | secret | **Reject here** | Out | — | — | Secret | Never on Personel Card | Adapter later | Adapter later | No | Integration credentials are not official employment data | Ref Firma “SGK KULLANICI BİLGİLERİ” |

Derived sicil parts (mahiyet, işkolu kodu, ünite kodu, sıra no, il, ilçe, CD, aracı) are **computable** from a 21–26 digit number in the reference. HuGuWeb does not persist them as separate required columns in 03B.

`IsActive` is **not** a uniqueness constraint. If a later configuration UI uses it to hide obsolete rows from pickers, that is UX metadata — not a “one active per Property” domain rule.

---

## Employment-level — Bildirge Kodları (HR-03B)

Owner: `OfficialEmploymentProfile` 1:0..1 on Employment. Current-value snapshot. All listed fields are **OPTIONAL** for HR-01B personnel creation and for ordinary Personel Card save.

| Product label (TR) | Technical | Type | Owner | Requiredness | Lookup/free-text | Effective-dated? | Sensitivity | Personel Card section | Future SGK | Future KBS | Excel later? | Validation notes | Source / evidence |
|--------------------|-----------|------|-------|--------------|------------------|------------------|-------------|-----------------------|------------|------------|--------------|------------------|-------------------|
| SGK İşyeri | SgkWorkplaceRegistrationId | Guid FK | `OfficialEmploymentProfile` / Employment | OPTIONAL (HUGUWEB). NEEDS LEGAL/EXPERT VALIDATION before SGK submit. | Select existing `SgkWorkplaceRegistration`. **Not** free-text sicil. | Snapshot on Employment. Mid-employment workplace change is an expert question; 03B overwrites current FK | Organization confidential / Sensitive HR | Resmî bilgiler → Bildirge Kodları | İşe giriş / hizmet workplace header | No | Restricted | When present: registration must exist; `registration.PropertyId` must correspond to the Property of the Employment’s relevant organizational context (Assignment → Department → Property). Do not type a new number here. | HuGuWeb decision (multiple registrations per Property). Ref placed sicil on Firma, not on every personel row. |
| Belge türü | DocumentTypeCode | string (lookup FK/code) | `OfficialEmploymentProfile` / Employment | OPTIONAL (HUGUWEB). REFERENCE REQUIRED only if a later SGK monthly/hizmet document is generated. NEEDS LEGAL/EXPERT VALIDATION before making hire-mandatory. | Lookup `SgkDocumentType`. Dropdown. | Snapshot on Employment. Intra-employment history FUTURE | Sensitive HR | Resmî bilgiler → Bildirge Kodları | Hizmet / monthly belge türü; işe giriş payload likely | No | Restricted | Code must exist in lookup when present. Store `01`, not display string. | Ref Personel Kartı Bildirge: label “Belge Türü”, `belgeTuru`, `<Select>` over `SGK_BELGE_TURLERI`. Empty “— Seçiniz —”. No asterisk. |
| Tabi olduğu kanun | ApplicableLawCode | string (lookup) | `OfficialEmploymentProfile` / Employment | OPTIONAL (HUGUWEB). NEEDS LEGAL/EXPERT VALIDATION before SGK submit (5510 vs 00000 vs incentive codes). | Lookup `ApplicableLawCode`. Dropdown. | Snapshot on Employment | Sensitive HR | Bildirge Kodları | Kanun no on hizmet / teşvik | No | Restricted | Code must exist in lookup when present. Store `05510`. | Ref label “Tabi Olduğu Kanun”, `tabiKanun`, options `kanun + " - " + aciklama` from `pdks_kanun_kodlari` / `_DEF_KANUNLAR`. Empty “— Seçiniz —”. No asterisk. |
| Sigorta kolu | InsuranceBranchCode | string (lookup) | `OfficialEmploymentProfile` / Employment | OPTIONAL (HUGUWEB). NEEDS LEGAL/EXPERT VALIDATION before SGK submit. | Lookup `InsuranceBranch`. Dropdown. | Snapshot on Employment | Sensitive HR | Bildirge Kodları | SIGORTAKOLU on işe giriş XML in ref | No | Restricted | Code must exist in lookup when present. Store `00`. | Ref label “Sigorta Kolu”, `sigortaKolu`, `<Select>` over `SGK_SIGORTA_KOLLARI`. Empty “— Seçiniz —”. No asterisk. |
| SGK meslek kodu | OccupationCode | string (lookup) | `OfficialEmploymentProfile` / Employment | OPTIONAL (HUGUWEB). REFERENCE REQUIRED for ref İŞKUR XML person row. NEEDS LEGAL/EXPERT VALIDATION before SGK işe giriş. | Lookup `SgkOccupationCode`. **Searchable** picker, not a giant native `<select>`. Catalogue is **importable**, not a 7765-row source seed. | Snapshot on Employment. Can change after promotion — history FUTURE | Sensitive HR | Bildirge Kodları | Meslek on işe giriş / İŞKUR meslek dağılımı | No | Restricted | When present: must exist in lookup; code length 7 (`NNNN.NN`) per source check. Store code only. Position does **not** own this code. | Ref label “SGK Meslek Kodu”, `sgkMeslekKodu`, `KodSecici` over 7765-row `_DEF_MESLEK`. Placeholder “Kod veya meslek yazın...”. Stores `kod + " - " + ad` in ref — **HuGuWeb stores code**. |

---

## Görev Kodu — discovery retained, out of HR-03B

**Disposition:** DEFERRED / NEEDS DOMAIN OR LEGAL VALIDATION. **Not** on OfficialEmploymentProfile in 03B. **Not** shown on Personel Card in 03B.

| Product label (TR) | Technical (ref) | Evidence | HuGuWeb now | Later |
|--------------------|-----------------|----------|-------------|-------|
| Görev kodu | `gorevKodu` / DutyCode | WebİK Bildirge grid exposes the field. Source contains **six Turkish labels** and **no separate numeric code** in the UI: İşveren veya Vekili; İşçi; 657 SK (4/b) Kapsamında Çalışanlar; 657 SK (4/c) Kapsamında Çalışanlar; Çıraklar ve Stajer Öğrenciler; Diğerleri. | Not modeled. Not a 03B lookup family. | May be added if domain/legal validation shows a stable statutory catalogue required by the SGK model. |

Do **not** delete this evidence. Do **not** treat the six labels as proven SGK payload identity.

---

## Explicitly not on OfficialEmploymentProfile (found on the same WebİK tab)

| Product label (TR) | Technical (ref) | HR-03A decision | Owner later | Why |
|--------------------|-----------------|-----------------|-------------|-----|
| SGK İşe Giriş Bildirildi | `sgkGirisBildirildi` | **Reject on profile** | Notification record | Accepted: government status ≠ Employment / Employee |
| SGK İşten Çıkış Bildirildi | `sgkCikisBildirildi` | **Reject on profile** | Notification record | Same |
| İşten Çıkış Kodu | `istenCikisKodu` | **Not this profile** | HR-02 End Employment / later SGK exit | Exit classification is a lifecycle command, not master edit |
| İşten Çıkış Kodu (İŞKUR) | `istenCikisKoduIskur` | **Not this profile** | HR-02 / İŞKUR later | Same; source list looks incomplete (5 defaults) |
| İŞKUR sözleşme türü / statü / kota | `sozlesmeTuru`, `iskurStatu`, … | **Not 03B UI** | Later official / HR-02 contract type | Prerequisite identification only |
| Teşvik / 5510 / indirim oranları | `tesvikKapsaminda5510`, … | **Reject** | Payroll | Personel Master already rejected payroll law on master |
| AGİ / BES | `agiHesapla`, `besKapsam` | **Reject** | Payroll | Same |
| Sosyal bilgiler / eğitim detayı | ehliyet, askerlik, okul | **Reject from this tab** | Personel Master already decided | Education level is HR-01B; rest deferred |
| Anne / baba adı, engellilik | `anneAdi`, `babaAdi`, disability | **Not 03B UI** | Later HR-03 official identity | FIELD_CATALOG B; hide empty sections |

---

## Person-level official number (not Bildirge Kodları)

| Product label (TR) | Technical | Owner | HR-03A |
|--------------------|-----------|-------|--------|
| Sos. Güv. No / SGK No | SocialSecurityNumber (`sgkNo` in ref) | Person-level if kept (survives rehire); **not** Property işyeri sicil | **Out of 03B.** Source placed it on Genel, default-hidden list column. Modern 4/a reporting typically uses TCKN. Keep as later official-identity if a spec still needs legacy SSK sicil. HighlySensitive. |

---

## Assignment / Position

| Concept | Owner | HR-03A |
|---------|-------|--------|
| Department / Position | Assignment (accepted) | Unchanged. Bildirge section does not transfer. |
| Authoritative SGK occupation | OfficialEmploymentProfile | **Decided.** Position ≠ OccupationCode. |
| Recommended SGK occupation on Position | Position | **FUTURE / NEEDS VALIDATION.** Suggestion only if ever added. Never automatic statutory truth. Do not add `DefaultOccupationCode` in 03B. Position.Code remains hotel-chosen short code, not ISCO. |

---

## Requiredness freeze (hire vs later submit)

| Moment | Official fields |
|--------|-----------------|
| Yeni Personel / Hire (HR-01B) | **Not required.** All Bildirge Kodları optional. HR-01B minimum unchanged. |
| Personel Card save of Resmî bilgiler | Codes optional; if provided, must match lookup / registration invariant. **Do not infer SGK submission completeness from this save.** |
| Future SGK işe giriş submit | NEEDS LEGAL/EXPERT VALIDATION — likely meslek kodu + belge türü + sigorta kolu + tabi kanun + workplace sicil + identity already on Personel Master. Not frozen as HUGUWEB REQUIRED in 03B. Completeness belongs to the later submit workflow. |
