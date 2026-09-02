namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Working-time classification. Distinct from <see cref="EmploymentContractType"/>.
/// </summary>
public enum WorkType
{
    FullTime = 1,
    PartTime = 2,
    ReducedHours = 3,
    Intern = 4
}
