using Launchpad.Web.Data;
using Launchpad.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Launchpad.Web.Features.Seeding;

public static class LaunchpadSeeder {
    public const string DemoPassword = "Launchpad!10";

    private static readonly (string Email, string DisplayName, string Title, string Role)[] Users =
    [
        ("admin@launchpad.local", "Ari Admin", "Studio director", LaunchpadRoles.Admin),
        ("producer@launchpad.local", "Parker Producer", "Release producer", LaunchpadRoles.Producer),
        ("dev@launchpad.local", "Devin Developer", "Gameplay engineer", LaunchpadRoles.Developer),
        ("qa@launchpad.local", "Quinn QA", "QA lead", LaunchpadRoles.QA),
        ("observer@launchpad.local", "Ollie Observer", "Publisher viewer", LaunchpadRoles.Observer),
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) {
        ApplicationDbContext db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (string role in LaunchpadRoles.All) {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false)) {
                await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
            }
        }

        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        foreach ((string Email, string DisplayName, string Title, string Role) in Users) {
            ApplicationUser? user = await userManager.FindByEmailAsync(Email).ConfigureAwait(false);
            if (user is null) {
                user = new ApplicationUser {
                    UserName = Email,
                    Email = Email,
                    EmailConfirmed = true,
                    DisplayName = DisplayName,
                    StudioTitle = Title,
                };
                IdentityResult created = await userManager.CreateAsync(user, DemoPassword).ConfigureAwait(false);
                if (!created.Succeeded) {
                    throw new InvalidOperationException(string.Join("; ", created.Errors.Select(error => error.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, Role).ConfigureAwait(false)) {
                await userManager.AddToRoleAsync(user, Role).ConfigureAwait(false);
            }
        }

        if (await db.GameProjects.AnyAsync(cancellationToken).ConfigureAwait(false)) {
            return;
        }

        GameProject project = new() {
            Id = Guid.NewGuid(),
            Name = "Starfall Tactics",
            CodeName = "starfall-tactics",
            Summary = "A tactics RPG about rival sky crews racing through a collapsing constellation.",
        };

        GameRelease release = new() {
            Id = Guid.NewGuid(),
            Project = project,
            Version = "v1.0 Launch Candidate",
            Codename = "Comet Crown",
            TargetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Summary = "The release candidate for the first public launch.",
        };

        release.Gates.AddRange([
            new ReleaseGate
            {
                Id = Guid.NewGuid(),
                Name = "Build",
                Description = "Compile server, client, content bundles, and release artifacts.",
                SortOrder = 1,
            },
            new ReleaseGate
            {
                Id = Guid.NewGuid(),
                Name = "QA",
                Description = "Run smoke tests for tutorial, combat, save/load, and controller flows.",
                SortOrder = 2,
            },
            new ReleaseGate
            {
                Id = Guid.NewGuid(),
                Name = "Security",
                Description = "Check dependencies, secrets, admin routes, and deployment configuration.",
                SortOrder = 3
            },
        ]);

        release.Bugs.AddRange([
            new BugReport
            {
                Id = Guid.NewGuid(),
                Title = "Controller remap menu loses focus after rebinding",
                Description = "QA can finish the flow, but keyboard/controller focus is inconsistent.",
                Severity = BugSeverity.High,
                Status = BugStatus.Triaged,
            },
            new BugReport
            {
                Id = Guid.NewGuid(),
                Title = "Final boss intro subtitle timing is off",
                Description = "Subtitle appears two seconds early in French locale.",
                Severity = BugSeverity.Medium,
                Status = BugStatus.InProgress,
            },
            new BugReport
            {
                Id = Guid.NewGuid(),
                Title = "Save file migration needs rollback note",
                Description = "The migration works, but support needs the rollback note linked from the release.",
                Severity = BugSeverity.Low,
                Status = BugStatus.New
            },
        ]);

        project.Feedback.AddRange([
            new PlaytestFeedback
            {
                Id = Guid.NewGuid(),
                Release = release,
                TesterAlias = "NebulaFox",
                Sentiment = FeedbackSentiment.Positive,
                Body = "The new tactical preview makes risky moves much easier to understand.",
                Source = "Seed",
            },
            new PlaytestFeedback
            {
                Id = Guid.NewGuid(),
                Release = release,
                TesterAlias = "OrbitAce",
                Sentiment = FeedbackSentiment.Mixed,
                Body = "Combat is great, but the controller menu still feels fragile.",
                Source = "Seed"
            },
        ]);

        db.GameProjects.Add(project);
        db.TeamNotifications.Add(new TeamNotification {
            Title = "Launchpad seeded",
            Body = "Open the War Room, start release checks, and ship Starfall Tactics.",
        });
        db.AuditEvents.Add(new AuditEvent {
            Action = "seed.created",
            EntityType = nameof(GameProject),
            EntityId = project.Id.ToString(),
            Detail = "Seeded Starfall Tactics release command center.",
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
