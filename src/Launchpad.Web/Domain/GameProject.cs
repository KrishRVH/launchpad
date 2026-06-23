namespace Launchpad.Web.Domain;

public sealed class GameProject {
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string CodeName { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<GameRelease> Releases { get; set; } = [];
    public List<PlaytestFeedback> Feedback { get; set; } = [];
}
