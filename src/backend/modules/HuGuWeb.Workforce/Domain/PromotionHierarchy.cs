namespace HuGuWeb.Workforce.Domain;

public static class PromotionHierarchy
{
    public static bool IsHigherLevel(int sourceOrganizationalLevel, int targetOrganizationalLevel) =>
        targetOrganizationalLevel > sourceOrganizationalLevel;
}
