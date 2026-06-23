using Launchpad.ServiceDefaults;
using Launchpad.Web.Components;
using Launchpad.Web.Components.Account;
using Launchpad.Web.Data;
using Launchpad.Web.Domain;
using Launchpad.Web.Features.Integrations;
using Launchpad.Web.Features.Notifications;
using Launchpad.Web.Features.Releases;
using Launchpad.Web.Features.Seeding;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, IntegrationApiKeyAuthenticationHandler>(IntegrationApiKeyDefaults.AuthenticationScheme, _ => { });

string connectionString = builder.Configuration.GetConnectionString("launchpaddb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'launchpaddb' or 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddSingleton<LaunchpadNotifier>();
builder.Services.AddSingleton<ReleaseCheckSignal>();
builder.Services.AddScoped<ReleaseWorkflowService>();
builder.Services.AddHostedService<ReleaseCheckWorker>();
builder.Services.AddHostedService<StudioReminderWorker>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(LaunchpadPolicies.ManageRelease, policy => policy.RequireRole(LaunchpadRoles.Admin, LaunchpadRoles.Producer))
    .AddPolicy(LaunchpadPolicies.RunChecks, policy => policy.RequireRole(LaunchpadRoles.Admin, LaunchpadRoles.Producer, LaunchpadRoles.Developer, LaunchpadRoles.QA))
    .AddPolicy(LaunchpadPolicies.ManageQa, policy => policy.RequireRole(LaunchpadRoles.Admin, LaunchpadRoles.Producer, LaunchpadRoles.QA))
    .AddPolicy(LaunchpadPolicies.IntegrationWrite, policy => {
        policy.AddAuthenticationSchemes(IntegrationApiKeyDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser();
    })
    .AddPolicy(LaunchpadPolicies.ViewAdmin, policy => policy.RequireRole(LaunchpadRoles.Admin));
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseMigrationsEndPoint();
    app.MapOpenApi();
} else {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.Use(async (context, next) => {
    IHeaderDictionary headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    await next().ConfigureAwait(false);
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.MapIntegrationApi();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Launchpad:SeedDemoData")) {
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await LaunchpadSeeder.SeedAsync(scope.ServiceProvider).ConfigureAwait(false);
}

await app.RunAsync();
