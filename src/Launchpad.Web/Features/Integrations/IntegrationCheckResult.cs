namespace Launchpad.Web.Features.Integrations;

public sealed record IntegrationCheckResult(bool Passed, string? Log);
