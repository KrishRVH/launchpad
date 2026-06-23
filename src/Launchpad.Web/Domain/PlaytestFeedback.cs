namespace Launchpad.Web.Domain;

public sealed class PlaytestFeedback {
    public Guid Id { get; set; }
    public Guid GameProjectId { get; set; }
    public GameProject Project { get; set; } = null!;
    public Guid? GameReleaseId { get; set; }
    public GameRelease? Release { get; set; }
    public string TesterAlias { get; set; } = "";
    public FeedbackSentiment Sentiment { get; set; } = FeedbackSentiment.Mixed;
    public string Body { get; set; } = "";
    public string Source { get; set; } = "Seed";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
