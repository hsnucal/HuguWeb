using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class HrProfileTests
{
    private static readonly byte[] JpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x01, 0x00, 0x48, 0x48, 0x00, 0x00, 0xFF, 0xD9
    ];

    [Fact]
    public async Task Create_WithProfile_CreatesEmployeeEmploymentAssignmentAndProfile()
    {
        var harness = new WorkforceHarness();
        var profile = Profile(
            educationLevel: EducationLevel.Bachelor,
            mobilePhone: "0555 111 22 33",
            email: "ayse@example.com");

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: profile),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Single(harness.Store.Employees);
        Assert.Single(harness.Store.Employments);
        Assert.Single(harness.Store.Assignments);
        Assert.Single(harness.Store.HrProfiles);
        Assert.Equal("05551112233", harness.Store.HrProfiles[0].MobilePhone);
        Assert.Equal("ayse@example.com", harness.Store.HrProfiles[0].Email);
        Assert.Equal(EducationLevel.Bachelor, harness.Store.HrProfiles[0].EducationLevel);
    }

    [Fact]
    public async Task Create_InvalidProfile_DoesNotLeaveEmployee()
    {
        var harness = new WorkforceHarness();
        var profile = Profile(email: "not-an-email");

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: profile),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-hr-profile", result.Error!.Code);
        Assert.NotNull(result.Error.Errors);
        Assert.Equal([HrValidation.Codes.EmailInvalid], result.Error.Errors[HrValidation.Fields.Email]);
        Assert.Empty(harness.Store.Employees);
        Assert.Empty(harness.Store.HrProfiles);
    }

    [Fact]
    public async Task Create_InvalidTckn_ReturnsNationalIdentityFieldError()
    {
        var harness = new WorkforceHarness();
        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: TcknProfile("12345")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-hr-profile", result.Error!.Code);
        Assert.Equal(
            [HrValidation.Codes.TcknLength],
            result.Error.Errors![HrValidation.Fields.NationalIdentityNumber]);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Create_InvalidPhone_ReturnsMobilePhoneFieldError()
    {
        var harness = new WorkforceHarness();
        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: Profile(mobilePhone: "12")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            [HrValidation.Codes.PhoneInvalid],
            result.Error!.Errors![HrValidation.Fields.MobilePhone]);
    }

    [Fact]
    public async Task Create_UnknownDepartment_ReturnsDepartmentFieldError()
    {
        var harness = new WorkforceHarness();
        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(departmentId: Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-not-found", result.Error!.Code);
        Assert.Equal(["department-not-found"], result.Error.Errors![HrValidation.Fields.DepartmentId]);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Create_UnknownPosition_ReturnsPositionFieldError()
    {
        var harness = new WorkforceHarness();
        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(positionId: Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-found", result.Error!.Code);
        Assert.Equal(["position-not-found"], result.Error.Errors![HrValidation.Fields.PositionId]);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Create_Passport_DoesNotUseTcknLengthRule()
    {
        var harness = new WorkforceHarness();
        var accepted = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: Profile(
                scheme: NationalIdentityScheme.Passport,
                number: "AB123456")),
            CancellationToken.None);

        Assert.True(accepted.IsSuccess, accepted.Error?.Detail);
        Assert.Equal(NationalIdentityScheme.Passport, harness.Store.HrProfiles[0].NationalIdentityScheme);

        var rejected = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: Profile(
                scheme: NationalIdentityScheme.Passport,
                number: "12")),
            CancellationToken.None);

        Assert.False(rejected.IsSuccess);
        Assert.Equal(
            [HrValidation.Codes.PassportFormat],
            rejected.Error!.Errors![HrValidation.Fields.NationalIdentityNumber]);
        Assert.DoesNotContain(
            HrValidation.Codes.TcknLength,
            rejected.Error.Errors[HrValidation.Fields.NationalIdentityNumber]);
    }

    [Fact]
    public async Task Update_ChangesOwnedProfileFields_NotEmployment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        var employmentStatus = harness.Store.Employments[0].Status;
        var departmentId = harness.Store.Assignments[0].DepartmentId;

        var result = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe Nur",
                "Yılmaz",
                Profile(mobilePhone: "05559998877", educationLevel: EducationLevel.Master),
                CanWriteSensitive: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal("Ayşe Nur", harness.Store.Employees[0].GivenName);
        Assert.Equal(hired.Value.PersonnelNumber, harness.Store.Employees[0].PersonnelNumber);
        Assert.Equal("05559998877", harness.Store.HrProfiles[0].MobilePhone);
        Assert.Equal(employmentStatus, harness.Store.Employments[0].Status);
        Assert.Equal(departmentId, harness.Store.Assignments[0].DepartmentId);
        Assert.Null(harness.Store.Employments[0].EndDate);
    }

    [Fact]
    public void Identity_NormalizesTcknAndRejectsInvalidFormat()
    {
        Assert.True(NationalIdentity.TryNormalize(
            NationalIdentityScheme.Tckn,
            "100 000 001 46",
            out var scheme,
            out var display,
            out var normalized,
            out _));
        Assert.Equal(NationalIdentityScheme.Tckn, scheme);
        Assert.Equal("10000000146", normalized);
        Assert.Equal("100 000 001 46", display);

        Assert.False(NationalIdentity.TryNormalize(
            NationalIdentityScheme.Tckn,
            "12345",
            out _,
            out _,
            out _,
            out var lengthError));
        Assert.Equal(HrValidation.Codes.TcknLength, lengthError);

        Assert.False(NationalIdentity.TryNormalize(
            NationalIdentityScheme.Tckn,
            "12345678901",
            out _,
            out _,
            out _,
            out var checksumError));
        Assert.Equal(HrValidation.Codes.TcknInvalid, checksumError);

        Assert.True(NationalIdentity.TryNormalize(
            NationalIdentityScheme.Passport,
            "A12345",
            out var passportScheme,
            out _,
            out var passportNormalized,
            out _));
        Assert.Equal(NationalIdentityScheme.Passport, passportScheme);
        Assert.Equal("A12345", passportNormalized);

        Assert.False(NationalIdentity.TryNormalize(
            NationalIdentityScheme.Passport,
            "12",
            out _,
            out _,
            out _,
            out var passportError));
        Assert.Equal(HrValidation.Codes.PassportFormat, passportError);
        Assert.NotEqual(HrValidation.Codes.TcknLength, passportError);
    }

    [Fact]
    public async Task Identity_IsOptional()
    {
        var harness = new WorkforceHarness();
        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: Profile(nationality: "RU")),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Null(harness.Store.HrProfiles[0].NationalIdentityNumber);
        Assert.False(harness.Store.HrProfiles[0].HasNationalIdentity);
    }

    [Fact]
    public async Task DuplicateIdentity_WithinOrganization_IsRejected()
    {
        var harness = new WorkforceHarness();
        var first = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: TcknProfile("10000000146")),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Detail);

        var second = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: TcknProfile("10000000146")),
            CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal("national-identity-in-use", second.Error!.Code);
        Assert.Equal(
            ["national-identity-in-use"],
            second.Error.Errors![HrValidation.Fields.NationalIdentityNumber]);
        Assert.Single(harness.Store.Employees);
    }

    [Fact]
    public async Task SameIdentity_InAnotherOrganization_IsAllowed()
    {
        var harness = new WorkforceHarness();
        Assert.True((await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: TcknProfile("10000000146")),
            CancellationToken.None)).IsSuccess);

        var otherOrg = Guid.CreateVersion7();
        Assert.True(Employee.TryCreate(
            Guid.CreateVersion7(),
            otherOrg,
            "Elena",
            "Popov",
            "X-1",
            out var other,
            out _));
        var profile = EmployeeHrProfile.Create(Guid.CreateVersion7(), other!.Id, otherOrg);
        Assert.True(profile.TryApply(
            TcknValues("10000000146"),
            harness.Clock.Today,
            out _,
            out _));
        harness.Store.Employees.Add(other);
        harness.Store.HrProfiles.Add(profile);

        await harness.Store.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(2, harness.Store.HrProfiles.Count(item => item.NormalizedNationalIdentityNumber == "10000000146"));
    }

    [Fact]
    public async Task MultipleEmergencyContacts_AreStored_AndSinglePrimaryEnforced()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: Profile(contacts:
            [
                new EmergencyContactDraft(Guid.Empty, "Ali Kaya", "Eş", "05551112233", true),
                new EmergencyContactDraft(Guid.Empty, "Elif Kaya", "Kardeş", "05554445566", false)
            ])),
            CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal(2, harness.Store.EmergencyContacts.Count);
        Assert.Single(harness.Store.EmergencyContacts, item => item.IsPrimary);

        var rejected = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                Profile(contacts:
                [
                    new EmergencyContactDraft(Guid.Empty, "Ali Kaya", "Eş", "05551112233", true),
                    new EmergencyContactDraft(Guid.Empty, "Elif Kaya", "Kardeş", "05554445566", true)
                ]),
                CanWriteSensitive: true),
            CancellationToken.None);

        Assert.False(rejected.IsSuccess);
        Assert.Equal("invalid-emergency-contact", rejected.Error!.Code);
        Assert.Single(harness.Store.EmergencyContacts, item => item.IsPrimary);
    }

    [Fact]
    public async Task Termination_RetainsProfile()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: TcknProfile("10000000146")),
            CancellationToken.None);

        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value!.EmployeeId, harness.Clock.Today, EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        Assert.Single(harness.Store.Employees);
        Assert.Single(harness.Store.HrProfiles);
        Assert.Equal("10000000146", harness.Store.HrProfiles[0].NormalizedNationalIdentityNumber);
        Assert.Equal(EmploymentStatus.Ended, harness.Store.Employments[0].Status);
    }

    [Fact]
    public async Task SensitiveRead_IsOmittedWithoutPermission()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: TcknProfile("10000000146", address: "Gizli Sokak 1")),
            CancellationToken.None);

        var redacted = await harness.HrCard.ExecuteAsync(hired.Value!.EmployeeId, canReadSensitive: false, CancellationToken.None);
        var allowed = await harness.HrCard.ExecuteAsync(hired.Value.EmployeeId, canReadSensitive: true, CancellationToken.None);

        Assert.True(redacted.IsSuccess);
        Assert.Null(redacted.Value!.Profile.NationalIdentityNumber);
        Assert.Null(redacted.Value.Profile.ResidenceAddress);
        Assert.Empty(redacted.Value.Profile.EmergencyContacts);
        Assert.False(redacted.Value.CanReadSensitive);

        Assert.Equal("10000000146", allowed.Value!.Profile.NationalIdentityNumber);
        Assert.Equal("Gizli Sokak 1", allowed.Value.Profile.ResidenceAddress);
        Assert.True(allowed.Value.CanReadSensitive);
    }

    [Fact]
    public async Task SensitiveWrite_WithoutPermission_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);

        var result = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                TcknProfile("10000000146"),
                CanWriteSensitive: false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("sensitive-write-forbidden", result.Error!.Code);
        Assert.Null(harness.Store.HrProfiles[0].NationalIdentityNumber);
    }

    [Fact]
    public async Task Photo_ValidatesTypeAndSize_AndSupportsReplaceAndRemove()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);

        await using var invalid = new MemoryStream("not-an-image"u8.ToArray());
        var rejected = await harness.Photos.UploadAsync(
            hired.Value!.EmployeeId,
            invalid,
            "image/jpeg",
            12,
            CancellationToken.None);
        Assert.False(rejected.IsSuccess);
        Assert.Equal("invalid-photo", rejected.Error!.Code);
        Assert.Empty(harness.Store.Photos);

        await using var first = new MemoryStream(JpegBytes);
        var uploaded = await harness.Photos.UploadAsync(
            hired.Value.EmployeeId,
            first,
            "image/jpeg",
            JpegBytes.Length,
            CancellationToken.None);
        Assert.True(uploaded.IsSuccess, uploaded.Error?.Detail);
        Assert.Single(harness.Store.Photos);
        Assert.Single(harness.PhotoStorage.Files);
        var firstKey = harness.Store.Photos[0].StorageKey;

        await using var second = new MemoryStream(JpegBytes);
        var replaced = await harness.Photos.UploadAsync(
            hired.Value.EmployeeId,
            second,
            "image/jpeg",
            JpegBytes.Length,
            CancellationToken.None);
        Assert.True(replaced.IsSuccess, replaced.Error?.Detail);
        Assert.Single(harness.Store.Photos);
        Assert.NotEqual(firstKey, harness.Store.Photos[0].StorageKey);
        Assert.DoesNotContain(firstKey, harness.PhotoStorage.Files.Keys);

        var removed = await harness.Photos.RemoveAsync(hired.Value.EmployeeId, CancellationToken.None);
        Assert.True(removed.IsSuccess);
        Assert.Empty(harness.Store.Photos);
        Assert.Empty(harness.PhotoStorage.Files);
    }

    [Fact]
    public void PhotoStorageKey_RejectsTraversal()
    {
        Assert.True(EmployeePhotoFile.IsSafeStorageKey($"{Guid.CreateVersion7():N}.jpg"));
        Assert.False(EmployeePhotoFile.IsSafeStorageKey("../secret.jpg"));
        Assert.False(EmployeePhotoFile.IsSafeStorageKey("a.jpg"));
        Assert.False(EmployeePhotoFile.IsSafeStorageKey(@"..\photo.jpg"));
    }

    [Fact]
    public async Task Directory_OmitsSensitiveIdentityWithoutPermission()
    {
        var harness = new WorkforceHarness();
        await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: TcknProfile("10000000146")),
            CancellationToken.None);

        var hidden = await harness.HrDirectory.ExecuteAsync(canReadSensitive: false, CancellationToken.None);
        var shown = await harness.HrDirectory.ExecuteAsync(canReadSensitive: true, CancellationToken.None);

        Assert.Null(hidden.Value![0].NationalIdentityNumber);
        Assert.Equal("10000000146", shown.Value![0].NationalIdentityNumber);
    }

    [Fact]
    public async Task Directory_WithZeroEmployees_SucceedsWithEmptyList()
    {
        var harness = new WorkforceHarness();

        var result = await harness.HrDirectory.ExecuteAsync(canReadSensitive: true, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Directory_WithEmployees_StillReturnsItems()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        var result = await harness.HrDirectory.ExecuteAsync(canReadSensitive: true, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal(hired.Value!.EmployeeId, result.Value[0].EmployeeId);
    }

    private static HrProfileWriteModel Profile(
        EducationLevel? educationLevel = null,
        string? mobilePhone = null,
        string? email = null,
        string? nationality = null,
        NationalIdentityScheme? scheme = null,
        string? number = null,
        string? address = null,
        IReadOnlyList<EmergencyContactDraft>? contacts = null) =>
        new(
            scheme,
            number,
            nationality,
            null,
            null,
            null,
            null,
            null,
            educationLevel,
            mobilePhone,
            null,
            email,
            address,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            contacts ?? []);

    private static HrProfileWriteModel TcknProfile(string number, string? address = null) =>
        Profile(
            scheme: NationalIdentityScheme.Tckn,
            number: number,
            address: address,
            contacts: [new EmergencyContactDraft(Guid.Empty, "Ali Kaya", "Eş", "05551112233", true)]);

    private static EmployeeHrProfileValues TcknValues(string number) =>
        new(
            NationalIdentityScheme.Tckn,
            number,
            null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null);
}
