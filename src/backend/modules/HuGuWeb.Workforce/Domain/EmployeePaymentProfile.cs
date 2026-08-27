namespace HuGuWeb.Workforce.Domain;

public sealed class EmployeePaymentProfile
{
    public const int IbanMaxLength = 34;
    public const int BankNameMaxLength = 200;

    private EmployeePaymentProfile()
    {
        Iban = string.Empty;
    }

    private EmployeePaymentProfile(Guid id, Guid employeeId, Guid organizationId, string iban, string? bankName)
    {
        Id = id;
        EmployeeId = employeeId;
        OrganizationId = organizationId;
        Iban = iban;
        BankName = bankName;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Iban { get; private set; }
    public string? BankName { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid employeeId,
        Guid organizationId,
        string? iban,
        string? bankName,
        out EmployeePaymentProfile? profile,
        out string? error)
    {
        profile = null;
        if (!PaymentIban.TryNormalize(iban, out var normalizedIban, out error))
        {
            return false;
        }

        if (!TryNormalizeBankName(bankName, out var normalizedBankName, out error))
        {
            return false;
        }

        profile = new EmployeePaymentProfile(id, employeeId, organizationId, normalizedIban, normalizedBankName);
        return true;
    }

    public bool TryUpdate(string? iban, string? bankName, out string? error)
    {
        if (!PaymentIban.TryNormalize(iban, out var normalizedIban, out error))
        {
            return false;
        }

        if (!TryNormalizeBankName(bankName, out var normalizedBankName, out error))
        {
            return false;
        }

        Iban = normalizedIban;
        BankName = normalizedBankName;
        return true;
    }

    private static bool TryNormalizeBankName(string? bankName, out string? normalized, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(bankName))
        {
            normalized = null;
            return true;
        }

        var trimmed = bankName.Trim();
        if (trimmed.Length > BankNameMaxLength)
        {
            normalized = null;
            error = $"Bank name must be {BankNameMaxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        return true;
    }
}
