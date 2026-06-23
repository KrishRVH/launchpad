namespace Launchpad.Web.Domain;

public static class LaunchpadRoles {
    public const string Admin = "Admin";
    public const string Producer = "Producer";
    public const string Developer = "Developer";
    public const string QA = "QA";
    public const string Observer = "Observer";

    public static readonly string[] All = [Admin, Producer, Developer, QA, Observer];
}
