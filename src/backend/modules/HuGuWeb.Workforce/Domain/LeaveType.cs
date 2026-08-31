using System.Globalization;

namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Organization-owned leave category. System defaults carry a stable <see cref="SystemKind"/>;
/// hotel-created custom types have <c>SystemKind = null</c>. Semantics never depend on <see cref="Name"/>.
/// </summary>
public sealed class LeaveType
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 200;
    public const int UserIdMaxLength = 450;

    private LeaveType()
    {
        Code = string.Empty;
        Name = string.Empty;
        CreatedByUserId = string.Empty;
        UpdatedByUserId = string.Empty;
    }

    private LeaveType(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        LeaveTypeSystemKind? systemKind,
        bool tracksBalance,
        decimal? defaultRequestAmount,
        string actorUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        SystemKind = systemKind;
        TracksBalance = tracksBalance;
        DefaultRequestAmount = defaultRequestAmount;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = actorUserId;
        UpdatedAtUtc = createdAtUtc;
        UpdatedByUserId = actorUserId;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public LeaveTypeSystemKind? SystemKind { get; private set; }
    public bool TracksBalance { get; private set; }
    /// <summary>
    /// Optional day-based request default for UI/self-service prefill. Not entitlement, balance,
    /// or approval FinalAmount.
    /// </summary>
    public decimal? DefaultRequestAmount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string UpdatedByUserId { get; private set; }

    public static LeaveType CreateSystemDefault(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        LeaveTypeSystemKind systemKind,
        bool tracksBalance,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        decimal? defaultRequestAmount = null)
    {
        if (!TryNormalizeCode(code, out var normalizedCode, out _, out _))
        {
            throw new ArgumentException($"System leave type code '{code}' is invalid.", nameof(code));
        }

        if (!TryNormalizeName(name, out var normalizedName, out _, out _))
        {
            throw new ArgumentException($"System leave type name '{name}' is invalid.", nameof(name));
        }

        if (!TryNormalizeDefaultRequestAmount(defaultRequestAmount, out var normalizedDefault, out _, out _))
        {
            throw new ArgumentException(
                $"System leave type default request amount '{defaultRequestAmount}' is invalid.",
                nameof(defaultRequestAmount));
        }

        return new LeaveType(
            id,
            organizationId,
            normalizedCode,
            normalizedName,
            systemKind,
            tracksBalance,
            normalizedDefault,
            actorUserId,
            createdAtUtc);
    }

    /// <summary>Create a custom (non-system) leave type. <c>SystemKind</c> is always null.</summary>
    public static bool TryCreateCustom(
        Guid id,
        Guid organizationId,
        string? code,
        string? name,
        bool tracksBalance,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out LeaveType? leaveType,
        out string? field,
        out string? errorCode,
        decimal? defaultRequestAmount = null)
    {
        leaveType = null;
        if (!TryNormalizeCode(code, out var normalizedCode, out field, out errorCode))
        {
            return false;
        }

        if (!TryNormalizeName(name, out var normalizedName, out field, out errorCode))
        {
            return false;
        }

        if (!TryNormalizeDefaultRequestAmount(defaultRequestAmount, out var normalizedDefault, out field, out errorCode))
        {
            return false;
        }

        leaveType = new LeaveType(
            id,
            organizationId,
            normalizedCode,
            normalizedName,
            systemKind: null,
            tracksBalance,
            normalizedDefault,
            actorUserId,
            createdAtUtc);
        field = null;
        errorCode = null;
        return true;
    }

    public bool TryRename(string? name, string actorUserId, DateTimeOffset utcNow, out string? field, out string? errorCode)
    {
        if (!TryNormalizeName(name, out var normalized, out field, out errorCode))
        {
            return false;
        }

        Name = normalized;
        Touch(actorUserId, utcNow);
        return true;
    }

    /// <summary>
    /// Change <see cref="TracksBalance"/>. Rejected when the type already has historical usage,
    /// because that would silently reinterpret existing movements/records.
    /// </summary>
    public bool TrySetTracksBalance(
        bool tracksBalance,
        bool hasHistoricalUsage,
        string actorUserId,
        DateTimeOffset utcNow,
        out string? field,
        out string? errorCode)
    {
        field = null;
        errorCode = null;
        if (tracksBalance == TracksBalance)
        {
            Touch(actorUserId, utcNow);
            return true;
        }

        if (hasHistoricalUsage)
        {
            field = LeaveValidation.Fields.TracksBalance;
            errorCode = LeaveValidation.Codes.LeaveTypeHasHistory;
            return false;
        }

        TracksBalance = tracksBalance;
        Touch(actorUserId, utcNow);
        return true;
    }

    public bool TrySetDefaultRequestAmount(
        decimal? defaultRequestAmount,
        string actorUserId,
        DateTimeOffset utcNow,
        out string? field,
        out string? errorCode)
    {
        if (!TryNormalizeDefaultRequestAmount(defaultRequestAmount, out var normalized, out field, out errorCode))
        {
            return false;
        }

        DefaultRequestAmount = normalized;
        Touch(actorUserId, utcNow);
        return true;
    }

    public void SetActive(bool isActive, string actorUserId, DateTimeOffset utcNow)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Touch(actorUserId, utcNow);
    }

    public void Deactivate(string actorUserId, DateTimeOffset utcNow) => SetActive(false, actorUserId, utcNow);

    private void Touch(string actorUserId, DateTimeOffset utcNow)
    {
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;
    }

    public static bool TryNormalizeDefaultRequestAmount(
        decimal? defaultRequestAmount,
        out decimal? normalized,
        out string? field,
        out string? errorCode)
    {
        normalized = null;
        field = null;
        errorCode = null;
        if (defaultRequestAmount is null)
        {
            return true;
        }

        if (!LeaveAmount.IsValidPositive(defaultRequestAmount.Value))
        {
            field = LeaveValidation.Fields.DefaultRequestAmount;
            errorCode = LeaveValidation.Codes.LeaveTypeInvalidDefaultRequestAmount;
            return false;
        }

        normalized = defaultRequestAmount.Value;
        return true;
    }

    public static bool TryNormalizeCode(string? code, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = LeaveValidation.Fields.Code;
        if (string.IsNullOrWhiteSpace(code))
        {
            errorCode = LeaveValidation.Codes.LeaveTypeCodeRequired;
            return false;
        }

        var trimmed = code.Trim().ToLowerInvariant();
        if (trimmed.Length > CodeMaxLength)
        {
            errorCode = LeaveValidation.Codes.LeaveTypeCodeTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }

    public static bool TryNormalizeName(string? name, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = LeaveValidation.Fields.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            errorCode = LeaveValidation.Codes.LeaveTypeNameRequired;
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            errorCode = LeaveValidation.Codes.LeaveTypeNameTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }

    public static string NormalizeCodeForLookup(string code) =>
        code.Trim().ToLower(CultureInfo.InvariantCulture);
}
