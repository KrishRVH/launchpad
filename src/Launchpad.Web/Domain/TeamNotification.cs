namespace Launchpad.Web.Domain;

public sealed class TeamNotification {
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
