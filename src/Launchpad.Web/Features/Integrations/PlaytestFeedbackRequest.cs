namespace Launchpad.Web.Features.Integrations;

public sealed record PlaytestFeedbackRequest(
    string ProjectCodeName,
    string? ReleaseVersion,
    string TesterAlias,
    string Sentiment,
    string Body);
