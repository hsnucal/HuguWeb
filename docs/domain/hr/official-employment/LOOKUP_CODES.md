# Lookup / reference codes

> **Status:** Accepted — HR-03A. Catalogues extracted from the WebİK frontend snapshot. **Not** HuGuWeb legal truth.
>
> Principle: official code fields are controlled code systems, not arbitrary free text.
>
> Conceptual row: `Code`, `DisplayName`, optional `ValidFrom` / `ValidTo`, `IsActive`.
>
> Do **not** create a generic “all government codes” table. Use **explicit lookup types** for the few families below.

HuGuWeb persists **code only** on Employment/Property. UI shows `code + description` (reference stored the whole concatenated string — reject that as identity).

---

## Families in HR-03B

Keep **explicit** reference families. Do **not** create one generic `GovernmentCode` table.

Store the **stable code**, not display text, as authoritative identity. Labels/descriptions are reference metadata.

| Family | Type name (direction) | Control | Source completeness |
|--------|----------------------|---------|---------------------|
| Belge türü | `SgkDocumentType` | Dropdown | **Complete in snapshot** (`SGK_BELGE_TURLERI`, 21 values) |
| Tabi kanun | `ApplicableLawCode` | Dropdown | Default list in snapshot (`_DEF_KANUNLAR`, 25 values). Personel Card loads `pdks_kanun_kodlari` (user-editable). Treat as **maintained lookup**, not a forever-frozen enum |
| Sigorta kolu | `InsuranceBranch` | Dropdown | **Complete in snapshot** (`SGK_SIGORTA_KOLLARI`, 8 values) |
| Meslek kodu | `SgkOccupationCode` | Searchable picker | **Complete national-style list in snapshot** (7765 rows). HuGuWeb does **not** copy that list into docs or 03B application source seed |

**Görev kodu / `SgkDutyCode` is not an HR-03B lookup family.** Discovery evidence is retained below.

`ValidFrom` / `ValidTo` on lookup rows are allowed conceptually so obsolete incentive laws can be deactivated without deleting history. 03B may ship `IsActive` only.

---

## Meslek kodu strategy

Source facts:

- Full list exists in the snapshot: `_DEF_MESLEK`, **7765** rows.
- Format: `NNNN.NN` (length 7), e.g. `5120.10`.
- UI: searchable `KodSecici` (“Kod veya meslek yazın…”), AND-search on normalized tokens of code + name, max 100 visible hits.
- Display: `kod + " - " + ad`.
- Employee field in ref stored that **display string**.
- İŞKUR XML helper: empty meslek → error; length > 7 → error.
- Users could overlay local edits (`pdks_meslek_ek`); base list was treated as read-only.

| Strategy | Meaning | Verdict |
|----------|---------|---------|
| **A. Full official catalogue in repo/seed** | 7765 ISCO-like rows in application source / migrations | **Rejected.** Do not seed the full official catalogue into source-controlled application seed data. Couples HuGuWeb releases to a national list; includes irrelevant military titles |
| **B. Curated hotel-relevant subset in git** | Tens/low hundreds of hospitality codes as the only catalogue | Insufficient as system of record; official codes change |
| **C. Maintained / importable reference catalogue** | Lookup structure filled by import, ops bootstrap, or vendor/official file | **Decided.** System of record |

**Decided: C.** Employment stores OccupationCode only. HR-03B may implement the `SgkOccupationCode` reference structure and a **practical bootstrap/import** path (empty table + import, or a small non-authoritative bootstrap so the picker is not blank). Bootstrap is not the national catalogue and is not statutory completeness.

Hotel-relevant **examples** present in the source list (not a closed HuGuWeb enum): `1411.08` Otel Müdürü, `1411.02` Ön Büro Müdürü-Otel, `4224.03` Ön Büro Görevlisi (Otel Resepsiyoncusu), `5120.10` Aşçı, `3434.01` Aşçıbaşı, `1120.10` Genel Müdür-Eğlence, Lokanta, Otel. **SOURCE DOES NOT PROVIDE a hotel-only subset product** — these are grep samples from the full list.

### Catalogue versioning / update (HR-03B concern)

Official occupation lists change. HuGuWeb must not treat a git dump as the catalogue version.

| Concern | Direction |
|---------|-----------|
| Identity | `Code` is the stored identity on OfficialEmploymentProfile. Updating a description must not rewrite historical codes. |
| Version | Record catalogue source identity (file name / official list version / imported-at). 03B may keep this operational rather than a full versioning product. |
| Update | Replace/upsert via import. Deactivate obsolete codes (`IsActive` or ValidTo); do not delete codes still referenced by profiles. |
| Selection | New picks should prefer currently active rows. Existing profiles may keep a now-inactive code. |
| Release coupling | Application releases must not require committing thousands of official rows. |
| Completeness | Offline completeness, if ever required by an integration spec, is an import/ops problem — not a source-seed problem. |

---

## Belge türü — source options

Control: native `<select>`. Values in snapshot (stored as full string in ref; HuGuWeb code = prefix before ` - `):

```text
01 - AYLIK SİGORTA PRİM BİLDİRGESİ
02 - SOSYAL GÜVENLİK DESTEK PRİM BİLDİRGESİ
03 - DENİZ, BASIM, AZOT, ŞEKER
04 - YERALTI SÜREKLİ
05 - YERALTI GRUPLU
06 - YERÜSTÜ GRUPLU
07 - ÇIRAK/STAJYER ÖĞRENCİ
11 - Y.Ö.K.KISMİ İTİH. ÖĞRENCİ
12 - GEÇİCİ 20. MADDEYE TABİ OLANLAR
13 - AYLIK SİGORTA PRİM İŞSİZLİK HARİÇ
19 - CEZA İNFAZ KURUMLARI
28 - STAJYER AV./İŞÇİ
29 - İŞKUR MESLEK EDİNDİRME
32 - TARBİL
33 - İŞKUR TOP. İŞ PROG.
39 - YABANCI UYRUKLU
42 - STAJYER (4/a-b)
44 - İŞKUR GENÇLİK PROG.
46 - YURT DIŞI BORÇLANMA
48 - İŞKUR İŞBAŞI EĞİTİM
49 - 50 VE ÜZERİ
50 - Lise/Üni Stajyer
51 - Harp Malülü
55 - EV HİZMETLERİ
```

Hotel-typical candidates (not HUGUWEB REQUIRED defaults): `01` normal; `02` emekli SGDP; `07`/`50` stajyer. Do not hide the rest unless an expert subset is approved.

---

## Tabi kanun — source default options

Personel Card dropdown is **dynamically loaded** from `pdks_kanun_kodlari`. If that store is empty, the card list is empty. The Kanun Kodları admin screen falls back to `_DEF_KANUNLAR`:

| Code | Display (source) |
|------|------------------|
| 00000 | SİGORTALI BİR KANUNA TABİ DEĞİL |
| 04325 | SİGORTALI OLAĞANÜSTÜ HAL KANUNUNA TABİ |
| 04369 | SGORTALI SENDİKA İNDİRİMİ KANUNUNA TABİ |
| 04382 | SİGORTALI SAKATLIK İNDİRİMİ KANUNUNA TABİ |
| 04447 | 4447 SAYILI KANUN |
| 04747 | SİGORTALI BORÇ ERTELEME İNDİRİMİNE TABİ İSE |
| 04857 | SİGORTALI SAKATLIK, E.HÜKÜMLÜ-TERÖR İNDİRİMİ KANUNUNA TABİ İSE |
| 05084 | HAZİNE İNDİRİMİNE %100 |
| 05510 | HAZİNE İNDİRİMİ |
| 05921 | HAZİNE İNDİRİMİ %100 |
| 06111 | OZ IND |
| 06645 | İŞKUR İŞBAŞI EĞİTİM |
| 14857 | KONTENJAN SINIRI İÇİNDEKİ ÖZÜRLÜ İŞÇİ |
| 27103 | 27103 SAYILI KHK |
| 47473 | SENDİKALI SİGORTALI BORÇ ERTELEME İNDİRİMİNE TABİ |
| 54857 | %100 SİGORTALI SAKATLIK, E HÜKÜMLÜ-TERÖR İNDİRİMİ KANUNUNA TABİ |
| 85084 | HAZİNE İNDİRİMİ %80 |
| 46486 | 46846 SAYILI KHK |
| 16322 | YATIRIM BELGESİ TEŞVİKİ |
| 07252 | 4447/GEÇİCİ 26. MADDE (KÇÖ YARARLANANLAR) |
| 27256 | 27256 SAYILI TEŞVİK |
| 17103 | 17103 SAYILI KHK |
| 07256 | 7256 SAYILI KHK |
| 03294 | 3294 SAYILI KHK |
| 05746 | 05746-04691 SAYILI KHK |
| 02828 | 02828 SAYILI KHK |
| 15510 | EYT TEŞVİK İNDİRİMİ %5 |

**SOURCE DOES NOT PROVE this list is the current complete SGK kanun table.** It is a reference-product default, user-editable, and includes incentive codes that belong to payroll policy as well as classification. HuGuWeb 03B should seed this as a **starting lookup**, allow deactivate, and not treat incentive selection as payroll calculation.

Reference default pairing (E-Bildirge Kodları / bordro tipi, **payroll** — do not copy as HuGuWeb payroll): Normal → belge `01` + kanun `05510` + kol `00`; Emekli → `02` / `00000` / `08`; Stajyer → `07` / `00000` / `07`.

---

## Sigorta kolu — source options

```text
00 - Tüm Sigorta Kolları
07 - Çırak
08 - Sosyal Güvenlik Destek Primi
12 - U.Söz Olmayan Yab.Uyrk.Sigortalı
14 - Cezaevi Çalışanları
16 - İşkur Kursiyerleri
17 - İş Kaybı Tazminatı Alanlar
18 - YÖK ve ÖSYM Kısmi İstihdam
```

Hotel-typical: `00` normal; `08` emekli SGDP; `07` çırak/stajyer. Others remain in the lookup.

---

## Görev kodu — discovery retained, **not HR-03B**

**Disposition:** **DEFERRED / NEEDS DOMAIN OR LEGAL VALIDATION.** Out of the HR-03B minimum. Not a 03B lookup family. Not stored on OfficialEmploymentProfile. Not shown on Personel Card.

**Why deferred:** WebİK exposes the field, but the snapshot does **not** establish that this is a stable official statutory code catalogue required by our SGK model. Six Turkish **labels**, no separate numeric code in the UI.

**Recorded facts (do not delete):**

- WebİK Personel Kartı Bildirge section exposes **Görev Kodu** (`gorevKodu`).
- Source contains **six** labels:

```text
İşveren veya Vekili
İşçi
657 SK (4/b) Kapsamında Çalışanlar
657 SK (4/c) Kapsamında Çalışanlar
Çıraklar ve Stajer Öğrenciler
Diğerleri
```

- HuGuWeb does **not** yet model it.
- It can be added later if validated as a required statutory catalogue.

If ever modeled, prefer stable English identifiers internally (e.g. `EmployerOrProxy`, `Worker`, `Law657B`, `Law657C`, `ApprenticeOrIntern`, `Other`) and translated labels — do not store the Turkish sentence as the code if a stable identity can be assigned. That implementation choice is **not** authorized in 03B.

---

## Exit codes (not 03B lookups to wire on the card)

### SGK işten çıkış (`_DEF_EK2`) — 46 rows in snapshot

| Kod | Açıklama (source) |
|-----|-------------------|
| 01 | Deneme süreli iş sözleşmesinin işveren tarafından feshi |
| 02 | Deneme süreli iş sözleşmesinin işçi tarafından feshi |
| 03 | Belirsiz süreli iş sözleşmesinin işçi tarafından feshi (istifa) |
| 04 | Belirsiz süreli iş sözleşmesinin işveren tarafından haklı sebep bildirilmeden feshi |
| 05 | Belirli süreli iş sözleşmesinin sona ermesi |
| 08 | Emeklilik (yaşlılık) veya toptan ödeme nedeniyle |
| 09 | Malulen emeklilik nedeniyle |
| 10 | Ölüm |
| 11 | İş kazası sonucu ölüm |
| 12 | Askerlik |
| 13 | Kadın işçinin evlenmesi |
| 14 | Emeklilik için yaş dışında diğer şartların tamamlanması |
| 15 | Toplu işçi çıkarma |
| 16 | Sözleşme sona ermeden sigortalının aynı işverene ait diğer işyerine nakli |
| 17 | İşyerinin kapanması |
| 18 | İşin sona ermesi |
| 19 | Mevsim bitimi (İş akdinin askıya alınması halinde kullanılır.Tekrar başlatılmayacaksa “4” nolu kod kullanılır) |
| 20 | Kampanya bitimi (İş akdinin askıya alınması halinde kullanılır. başlatılmayacaksa “4” nolu kod kullanılır) |
| 21 | Statü değişikliği |
| 22 | Diğer nedenler |
| 23 | İşçi tarafından zorunlu nedenle fesih |
| 24 | İşçi tarafından sağlık nedeniyle fesih |
| 25 | İşçi tarafından işverenin ahlak ve iyi niyet kurallarına aykırı davranışı nedeni ile fesih |
| 26 | Disiplin kurulu kararı ile fesih |
| 27 | İşveren tarafından zorunlu nedenlerle ve tutukluluk nedeniyle fesih |
| 28 | İşveren tarafından sağlık nedeni ile fesih |
| 29 | İşveren tarafından işçinin ahlak ve iyi niyet kurallarına aykırı davranışı nedeni ile fesih |
| 30 | Vize süresinin bitimi |
| 31 | Borçlar Kanunu, Sendikalar Kanunu, Grev ve Lokavt Kanunu kapsamında kendi istek ve kusuru dışında feshi |
| 32 | 4046 sayılı Kanunun 21’inci maddesine göre özelleştirme nedeni ile fesih |
| 33 | Gazeteci tarafından sözleşmenin feshi |
| 34 | İşyerinin devri, işin veya işyerinin niteliğinin değişmesi nedeniyle fesih |
| 37 | KHK ile kamu görevinden çıkarma |
| 38 | Doğum nedeniyle işten ayrılma |
| 39 | 696 KHK ile kamu işçiliğine geçiş |
| 40 | 696 KHK ile kamu işçiliğine geçilmemesi sebebiyle çıkış |
| 41 | SGK tarafından değişik gerekçeler nedeniyle işten ayrılışları re’sen düzenlenenler için seçilecek koddur. |
| 42 | 4857 sayılı Kanun Madde 25-II-a |
| 43 | 4857 sayılı Kanun Madde 25-II-b |
| 44 | 4857 sayılı Kanun Madde 25-II-c |
| 45 | 4857 sayılı Kanun Madde 25-II-d |
| 46 | 4857 sayılı Kanun Madde 25-II-e |
| 47 | 4857 sayılı Kanun Madde 25-II-f |
| 48 | 4857 sayılı Kanun Madde 25-II-g |
| 49 | 4857 sayılı Kanun Madde 25-II-h |
| 50 | 4857 sayılı Kanun Madde 25-II-ı |

User-editable in ref (`pdks_ek2_ayrilik`). **Do not wire onto OfficialEmploymentProfile.** Keep this catalogue for HR-02 / SGK exit.

### İŞKUR işten çıkış (`_DEF_ISKUR`) — 5 default rows

| No | Açıklama (source) |
|----|-------------------|
| 1 | 4447/51-a 1475/13 İşveren-4857/17 İşveren-4857/18 |
| 2 | 4447/51-b 1475/16 ve 4857 24. maddelerini kapsamak |
| 3 | 4447/51-c 1475/17 ve 4857 25. maddelerini kapsamak |
| 4 | 4447/51-d İşBitimi-İhaleli İşin Sona Ermesi-Belir |
| 5 | 4447/51-e İşyerinin Kapanması |

**SOURCE DOES NOT PROVIDE A COMPLETE İŞKUR EXIT LIST** in the snapshot (only these defaults; user-editable). Do not invent the rest.

### İşe giriş official codes

**SOURCE DOES NOT PROVIDE** a Personel Card entry-reason dropdown. Do not fabricate SGK işe giriş nedeni values.

---

## ÇSGB iş kolu (Property, not employee)

Source Firma “İş Kolu” (20 options). Hotel-relevant: `18 - Konaklama ve Eğlence İşleri`. **Out of 03B minimum** (prefer parse-from-sicil later). Full list is in the snapshot; not copied here to avoid expanding Property.

---

## What not to lookup-ify

Payroll flags, AGİ/BES, incentive percentages, SGK notified booleans, KBS unit, credentials.
