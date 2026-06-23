namespace Launchpad.PlaywrightTests;

[TestClass]
public sealed class WarRoomSmokeTests : PageTest {
    [TestMethod]
    public async Task WarRoomShowsLaunchpadTitle() {
        string? baseUrl = Environment.GetEnvironmentVariable("LAUNCHPAD_E2E_URL");
        if (string.IsNullOrWhiteSpace(baseUrl)) {
            Assert.Inconclusive("Set LAUNCHPAD_E2E_URL to a running Launchpad app to run smoke tests.");
        }

        await Page.GotoAsync(baseUrl);
        await Expect(Page.GetByText("Launchpad", new() { Exact = false })).ToBeVisibleAsync();
    }
}
