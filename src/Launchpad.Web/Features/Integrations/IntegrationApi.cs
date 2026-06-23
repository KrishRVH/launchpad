using Launchpad.Web.Domain;
using Launchpad.Web.Features.Releases;

namespace Launchpad.Web.Features.Integrations;

public static class IntegrationApi {
    public static IEndpointRouteBuilder MapIntegrationApi(this IEndpointRouteBuilder endpoints) {
        RouteGroupBuilder group = endpoints.MapGroup("/api/integrations")
            .WithTags("Integrations")
            .RequireAuthorization(LaunchpadPolicies.IntegrationWrite);

        group.MapPost("/check-runs/{id:guid}/result", async (
                Guid id,
                IntegrationCheckResult request,
                ReleaseWorkflowService workflow,
                CancellationToken cancellationToken) => {
                    ReleaseWorkflowResult result = await workflow.RecordExternalCheckResultAsync(id, request.Passed, request.Log ?? "", cancellationToken).ConfigureAwait(false);
                    return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
                })
            .WithName("SubmitReleaseCheckResult");

        group.MapPost("/playtest-feedback", async (
                PlaytestFeedbackRequest request,
                ReleaseWorkflowService workflow,
                CancellationToken cancellationToken) => {
                    if (string.IsNullOrWhiteSpace(request.Body)) {
                        return Results.BadRequest("Feedback body is required.");
                    }

                    FeedbackSentiment sentiment = Enum.TryParse(request.Sentiment, ignoreCase: true, out FeedbackSentiment parsed)
                        ? parsed
                        : FeedbackSentiment.Mixed;

                    ReleaseWorkflowResult result = await workflow.SubmitFeedbackAsync(
                        request.ProjectCodeName,
                        request.ReleaseVersion,
                        request.TesterAlias,
                        sentiment,
                        request.Body,
                        "Integration API",
                        cancellationToken).ConfigureAwait(false);

                    return result.Succeeded ? Results.Accepted(value: result) : Results.BadRequest(result);
                })
            .WithName("IngestPlaytestFeedback");

        return endpoints;
    }
}
