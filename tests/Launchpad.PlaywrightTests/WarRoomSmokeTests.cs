namespace Launchpad.PlaywrightTests;

[TestClass]
public sealed class WarRoomSmokeTests : PageTest {
    [TestMethod]
    public async Task ProducerCanOpenWarRoom() {
        string baseUrl = RequiredBaseUrl();

        await Page.GotoAsync($"{baseUrl}/Account/Login?ReturnUrl=%2F");
        await Page.GetByLabel("Email").FillAsync("producer@launchpad.local");
        await Page.GetByLabel("Password").FillAsync("Launchpad!10");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Starfall Tactics v1.0 Launch Candidate" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Start release checks" })).ToBeEnabledAsync();
    }

    private static string RequiredBaseUrl() {
        string? baseUrl = Environment.GetEnvironmentVariable("LAUNCHPAD_E2E_URL")?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) {
            Assert.Inconclusive("Set LAUNCHPAD_E2E_URL to a running Launchpad app to run smoke tests.");
        }

        return baseUrl;
    }
}
