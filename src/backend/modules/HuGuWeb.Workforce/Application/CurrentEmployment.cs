using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class CurrentEmployment
{
    public static WorkforceResult<Employment> Find(IReadOnlyList<Employment> employments)
    {
        var open = employments.Where(item => !item.IsEnded).ToArray();
        if (open.Length == 0)
        {
            return WorkforceError.NoCurrentEmployment();
        }

        if (open.Length > 1)
        {
            return WorkforceError.MultipleOpenEmployments();
        }

        return open[0];
    }

    public static Employment? TryFind(IEnumerable<Employment> employments)
    {
        var open = employments.Where(item => !item.IsEnded).Take(2).ToArray();
        return open.Length == 1 ? open[0] : null;
    }
}
