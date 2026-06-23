namespace Launchpad.Web.Features.Releases;

public readonly record struct ReleaseCheckClaim(Guid RunId, string Kind);
