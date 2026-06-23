namespace Launchpad.Web.Features.Releases;

public sealed record ReleaseWorkflowResult(bool Succeeded, string Message) {
    public static ReleaseWorkflowResult Ok(string message) => new(Succeeded: true, message);
    public static ReleaseWorkflowResult Fail(string message) => new(Succeeded: false, message);
}
