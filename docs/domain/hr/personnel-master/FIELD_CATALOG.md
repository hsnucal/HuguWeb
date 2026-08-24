# Personel Master field catalog

> **Status:** Accepted — HR-01A. Catalog only. **Not** schema, EF, or API contracts.
>
> **HR-01 column:** `A` = Personel Master (this freeze) · `B` = later official/government slice · `C` = defer · `D` = reject
>
> **List / Import / Export:** `Yes` = eligible under normal HR permissions · `Restricted` = only with `hr.employee.sensitive.read` (and never default) · `No`

Source/reference notes are **REFERENCE PRODUCT BEHAVIOR** unless marked HuGuWeb decision.

---

## Employee core

| Label (TR) | Technical | Type | Required | Owner | HR-01 | Sensitivity | List | Import | Export | Validation / notes | Source |
|------------|-----------|------|----------|-------|-------|-------------|------|--------|--------|---------------------|--------|
| (system) | EmployeeId | Guid | yes | Employee | A | Normal | No | No | No | PK. Never shown. | Accepted |
| (system) | OrganizationId | Guid | yes | Employee | A | Normal | No | No | No | Uniqueness scope | Accepted |
| Sicil No | PersonnelNumber | string | yes | Employee | A | Normal | Yes | Yes | Yes | Unique in Organization; never reused; not PK; manual | Accepted; ref auto-numbers — **HuGuWeb stays manual** |
| Ad | GivenName | string | yes | Employee | A | Normal | Yes | Yes | Yes | Trim; max length; do not overvalidate names | Accepted |
| Soyad | FamilyName | string | yes | Employee | A | Normal | Yes | Yes | Yes | Same | Accepted |
| Ad soyad | DisplayName | derived | — | Employee | A | Normal | Yes | No | Yes | Composition, not stored separately | HuGuWeb |

---

## Photo

| Label (TR) | Technical | Type | Required | Owner | HR-01 | Sensitivity | List | Import | Export | Validation / notes | Source |
|------------|-----------|------|----------|-------|-------|-------------|------|--------|--------|---------------------|--------|
| Fotoğraf | PhotoObjectKey | string | no | EmployeePhoto | A | Sensitive | Yes (thumbnail for HR) | No (bulk photo workflow) | No | Metadata + storage; not base64 on Employee; type/size at implement | Ref card + bulk photo |
| | ContentType | string | if photo | EmployeePhoto | A | Normal | No | No | No | | |
| | ByteSize | int | if photo | EmployeePhoto | A | Normal | No | No | No | | |
| | UploadedAt | datetime | if photo | EmployeePhoto | A | Normal | No | No | No | | |

---

## Identity / demographics

National identity model: `NationalIdentityScheme` + `NationalIdentityNumber`. Do not create a generic identity platform.

| Label (TR) | Technical | Type | Required | Owner | HR-01 | Sensitivity | List | Import | Export | Validation / notes | Source |
|------------|-----------|------|----------|-------|-------|-------------|------|--------|--------|---------------------|--------|
| Kimlik şeması | NationalIdentityScheme | enum: Tckn, Ykn, Passport, Other | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | Required **if** number present | HuGuWeb; ref assumed TCKN |
| TCKN / YKN / Pasaport no | NationalIdentityNumber | string | no | EmployeeHrProfile | A | HighlySensitive | Restricted | Restricted | Restricted | Unique when present as **Organization + Scheme + normalized identifier**; TCKN format+checksum if scheme=Tckn; **optional**; not PK | Ref required TCKN — **HuGuWeb rejects required** |
| Kimlik belgesi türü | IdentityDocumentType | enum | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | TcknCard, IdentityBooklet, Passport, DrivingLicence, ForeignId, Other | Ref |
| Uyruk | Nationality | string or ISO code | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | Prefer ISO country code later; working string OK in 01B | Ref; KBS/İŞKUR prep |
| Cinsiyet | Gender | enum | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | Female, Male, Unspecified. Do not use for authorization | Ref; official prep |
| Doğum tarihi | BirthDate | date | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | Sensible range; no arbitrary hotel age policy | Ref; KBS |
| Doğum yeri | BirthPlace | string | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | | Ref; KBS |
| Kan grubu | BloodType | enum | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | Optional HR-01B. A+/A-/… ; not authorization; not default list; not operational APIs | Ref |
| Medeni hali | MaritalStatus | enum | no | EmployeeHrProfile | A | Sensitive | No | Restricted | Restricted | Single, Married, Divorced, Widowed, Unspecified | Ref; later payroll AGİ is **not** this field’s job |
| Kimlik seri no | IdentitySerialNumber | string | no | EmployeeHrProfile | B | HighlySensitive | No | No | Restricted | Legacy booklet/card; keep only if KBS file still needs it | Ref |
| Cüzdan / belge no | IdentityBookletNumber | string | no | EmployeeHrProfile | B | HighlySensitive | No | No | Restricted | Same; do not collect “because the old card had it” | Ref |
| Anne adı | MotherGivenName | string | no | Official later | B | Sensitive | No | No | Restricted | **HR-03.** Not HR-01B | Ref |
| Baba adı | FatherGivenName | string | no | Official later | B | Sensitive | No | No | Restricted | **HR-03.** Not HR-01B | Ref |
| Nüfus kayıt no | CivilRegistryNumber | string | no | EmployeeHrProfile | B | HighlySensitive | No | No | Restricted | Official slice | Ref |
| Kayıtlı mahalle | RegisteredNeighborhood | string | no | EmployeeHrProfile | B | HighlySensitive | No | No | Restricted | Official / KBS | Ref |
| Evlilik tarihi | MarriageDate | date | no | EmployeeHrProfile | C | Sensitive | No | No | No | No current HR-01 consumer | Ref |
| Emeklilik tarihi | RetirementDate | date | no | EmployeeHrProfile | C | Sensitive | No | No | No | Not Employment.EndDate | Ref |
| Anne kızlık soyadı | MotherMaidenFamilyName | string | no | — | D | — | No | No | No | No hotel/official need identified beyond curiosity | Ref |
| HES kodu | — | — | — | — | D | — | No | No | No | Obsolete pandemic field | Ref |

---

## Contact / address / emergency

| Label (TR) | Technical | Type | Required | Owner | HR-01 | Sensitivity | List | Import | Export | Validation / notes | Source |
|------------|-----------|------|----------|-------|-------|-------------|------|--------|--------|---------------------|--------|
| Cep telefonu | MobilePhone | string | no | EmployeeHrProfile | A | Sensitive | No | Yes | Restricted | Normalize; E.164-ish later | Ref `telefon`/`cep` |
| Ev telefonu | HomePhone | string | no | EmployeeHrProfile | A | Sensitive | No | Yes | Restricted | | Ref |
| E-posta | Email | string | no | EmployeeHrProfile | A | Sensitive | No | Yes | Restricted | Format only | Ref |
| İkinci e-posta | EmailSecondary | string | no | — | C | Sensitive | No | No | No | Avoid until needed | Ref |
| KEP | RegisteredElectronicMail | string | no | — | C | Sensitive | No | No | No | Legal channel later | Ref |
| İkametgâh adres | ResidenceAddress | string | no | EmployeeHrProfile | A | HighlySensitive | Restricted | Restricted | Restricted | | Ref |
| İl | ResidenceCity | string | no | EmployeeHrProfile | A | HighlySensitive | No | Restricted | Restricted | TR city list is UI, not a HuGuWeb geo product | Ref |
| İlçe | ResidenceDistrict | string | no | EmployeeHrProfile | A | HighlySensitive | No | Restricted | Restricted | | Ref |
| Posta kodu | PostalCode | string | no | EmployeeHrProfile | C | HighlySensitive | No | No | No | | Ref `iceriAlinmaz` |
| İkametgâh adres 2 | ResidenceAddressLine2 | string | no | EmployeeHrProfile | C | HighlySensitive | No | No | No | One address line is enough until overflow exists | Ref |
| Barınma / bildirim adresi | NotificationAddress | string | no | EmployeeHrProfile | A | HighlySensitive | Restricted | Restricted | Restricted | Where the person actually stays; KBS prerequisite; may differ from residence | Ref |
| Tel kısa kod | PhoneShortCode | string | no | — | D | — | No | No | No | PBX leftover | Ref |
| Acil durum kişisi | EmergencyContact | collection | no | EmergencyContact | A | HighlySensitive | No | Restricted | Restricted | Name, Relationship, Phone, IsPrimary; ≥2 allowed; not Contact1/Contact2 columns | Ref used two flat pairs — **HuGuWeb collection** |
| Acil durum grubu | EmergencyGroup | string | no | — | C | Sensitive | No | No | No | Meaning unclear (not blood type). NEEDS VALIDATION. Not core | Ref |

---

## Organization / employment (existing + optional dates)

| Label (TR) | Technical | Type | Required | Owner | HR-01 | Sensitivity | List | Import | Export | Validation / notes | Source |
|------------|-----------|------|----------|-------|-------|-------------|------|--------|--------|---------------------|--------|
| Organizasyon | Organization name | display | — | Organization | A | Normal | No | No | Yes | Read-only; seeded | Ref “Firma” |
| Tesis | Property name | display | — | Property | A | Normal | No | No | Yes | Read-only | HuGuWeb |
| Departman | DepartmentId | Guid ref | yes on hire | Assignment | A | Normal | Yes | Yes (ref) | Yes | Never duplicate name on Employee | Accepted; ref bound görev to departman — **HuGuWeb does not** |
| Pozisyon | PositionId | Guid ref | yes on hire | Assignment | A | Normal | Yes | Yes (ref) | Yes | Independent of Department | Accepted |
| Bölüm | Section | — | — | — | C | Normal | No | No | No | Sub-department. Flat Department remains. Hierarchy later | Ref |
| Kademe | Grade | — | — | — | C | Normal | No | No | No | Deferred. Not HR-01B. Never grants permissions | Ref rank ladder |
| Çalışma grubu | WorkingGroup | — | — | — | D | Normal | No | No | No | Not Personel Master. Revisit as EmploymentClassification. Not Shift | Ref payroll bucket |
| İş ilişkisi durumu | EmploymentStatus | enum | yes | Employment | A | Normal | Yes | No | Yes | Scheduled/Active/Ended; not attendance | Accepted; ref “Çalışıyor/Ayrıldı/Askıda” |
| İşe giriş | Employment.StartDate | date | yes | Employment | A | Normal | Yes | Yes | Yes | Period rules | Accepted |
| İşten ayrılış | Employment.EndDate | date | if Ended | Employment | A | Normal | No | No | Yes | End command, not profile import | Accepted |
| Şirket giriş | OriginalCompanyStartDate | date | no | Employment | C | Normal | No | Yes | Yes | **HR-02.** ≠ Employment.StartDate when rehire/group history | Ref |
| Kıdeme esas giriş | SeniorityStartDate | date | no | Employment | C | Normal | No | Yes | Yes | **HR-02.** Leave/compensation later consume | Ref |
| SGK / SSK giriş tarihi | SocialSecurityEntryDate | date | no | Official later | B | Sensitive | No | No | Restricted | Notification vs business date | Ref |
| Çıkış nedeni | TerminationReason | — | — | HR-02 | C | Sensitive | No | No | No | | Ref |
| Bordro sicil | PayrollPersonnelNumber | — | — | Payroll | C | Sensitive | No | No | No | Do not split sicil without evidence | Ref |
| SGK işyeri / sigortalı no | SocialSecurityNumber | — | — | Official | B | HighlySensitive | No | No | Restricted | Not Employee PK | Ref |
| Devam kontrol | AttendanceTrackingFlag | — | — | Attendance | C | Normal | No | No | No | | Ref |
| Askıda | Suspended | — | — | HR-02 | C | Normal | No | No | No | Not an EmploymentStatus now | Ref |

---

## Education / disability / notes

| Label (TR) | Technical | Type | Required | Owner | HR-01 | Sensitivity | List | Import | Export | Validation / notes | Source |
|------------|-----------|------|----------|-------|-------|-------------|------|--------|--------|---------------------|--------|
| Öğrenim durumu | EducationLevel | enum | no | EmployeeHrProfile | A | Sensitive | No | Yes | Restricted | Optional HR-01B summary; Primary…Doctorate | Ref |
| Okul / mezuniyet / dil detayı | EmployeeEducation | records | — | Training/Career | C | Sensitive | No | No | No | Not Personel Master rows | Ref |
| Engellilik / derece | DisabilityDegree | enum | no | Official / legal | B | HighlySensitive | Restricted | Restricted | Restricted | **HR-03.** Quota/İŞKUR; not operational auth | Ref |
| Not | HrNotes | string | no | EmployeeHrProfile | A | Sensitive | No | No | No | Free text; no secrets policy in logs | Ref |

---

## Payment (model freeze; persist HR-01C)

| Label (TR) | Technical | Type | Required | Owner | HR-01 | Sensitivity | List | Import | Export | Validation / notes | Source |
|------------|-----------|------|----------|-------|-------|-------------|------|--------|--------|---------------------|--------|
| IBAN | Iban | string | no | EmployeePaymentProfile | A (persist 01C) | HighlySensitive | Restricted | Restricted | Restricted | Format checksum; never operational API | Ref |
| Banka adı | BankName | string | no | EmployeePaymentProfile | A (persist 01C) | HighlySensitive | No | Restricted | Restricted | Optional; not a bank master data product | Ref |
| Şube | BankBranch | string | no | EmployeePaymentProfile | C | HighlySensitive | No | No | No | Prefer IBAN-only until evidence | Ref |
| Hesap no | AccountNumber | string | no | EmployeePaymentProfile | C | HighlySensitive | No | No | No | Redundant with IBAN for modern payments | Ref |
| Güncel ücret | BaseWage | money | no | EmploymentCompensationTerms | C | HighlySensitive | No | No | Restricted | Compensation domain; not Employee | Ref |
| Net/Brüt | WageBasis | enum | no | EmploymentCompensationTerms | C | HighlySensitive | No | No | No | | Ref |
| Ücret tipi | WagePeriod | enum | no | EmploymentCompensationTerms | C | Sensitive | No | No | No | Monthly/daily/hourly | Ref |
| Küm. GV / AGİ / vergi | — | — | — | Payroll | D | — | No | No | No | Reject on Personel Master | Ref |
| FM devri / yıllık izin devri | — | — | — | Leave / attendance | D | — | No | No | No | Balances are not master identity | Ref |
| Avans / icra | — | — | — | Compensation | D | — | No | No | No | | Ref |

---

## Official codes (prerequisites vs lifecycle)

Collect **identity/contact prerequisites** in Personel Master. Do **not** store submission lifecycle on Employee.

| Label (TR) | Technical | HR-01 | Owner later |
|------------|-----------|-------|-------------|
| Belge türü, tabi kanun, sigorta kolu, meslek kodu | Official employment codes | B | HR-03 |
| SGK giriş/çıkış bildirildi flags | Notification state | D on Employee | HR-03 notification record |
| İŞKUR sözleşme türü, kota, işgücü durum | Official | B | HR-03 |
| BES / AGİ / 5510 teşvik | Payroll law | D on Personel Master | Payroll |
| Kimlik bildirimi (polis/jandarma) payload | Adapter | D | HR-03 |

Prerequisite **now** (A / HR-01B): names, sicil, nationality, gender, birth date/place, marital status, optional blood type, addresses (residence + optional notification/stay), national identity scheme/number, employment start, department/position **ids**.

Parent names, disability, identity booklet/serial/registry, and official codes: **HR-03**.

---

## Explicitly out (staff services / PDKS / portal)

| Ref field | Decision |
|-----------|----------|
| Dolap, lojman, servis, durak, cihaz no | C — assets / transport / PDKS, not Personel Master |
| Etiketler | C — no generic tagging platform |
| Portal username / password-from-TCKN | D — Employee ≠ User; never seed passwords from TCKN |
| DISC | D — out of scope |

---

## Documents / history (not Employee columns)

| Concept | HR-01 | Notes |
|---------|-------|--------|
| Attachment metadata on Employee | **No** | HR-04 |
| Employment/Assignment history | Already exists | Card Geçmiş tab |
| Profile change history | C (HR-01C) | Narrow, not enterprise audit |
