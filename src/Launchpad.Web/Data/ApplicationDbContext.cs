using Launchpad.Web.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Launchpad.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options) {
    public DbSet<GameProject> GameProjects => Set<GameProject>();
    public DbSet<GameRelease> GameReleases => Set<GameRelease>();
    public DbSet<ReleaseGate> ReleaseGates => Set<ReleaseGate>();
    public DbSet<ReleaseCheckRun> ReleaseCheckRuns => Set<ReleaseCheckRun>();
    public DbSet<BugReport> BugReports => Set<BugReport>();
    public DbSet<PlaytestFeedback> PlaytestFeedback => Set<PlaytestFeedback>();
    public DbSet<TeamNotification> TeamNotifications => Set<TeamNotification>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);

        builder.Entity<GameProject>(entity => {
            entity.HasIndex(project => project.CodeName).IsUnique();
            entity.Property(project => project.Name).HasMaxLength(120);
            entity.Property(project => project.CodeName).HasMaxLength(80);
        })
            .Entity<GameRelease>(entity => {
                entity.HasIndex(release => new { release.GameProjectId, release.Version }).IsUnique();
                entity.Property(release => release.Status).HasConversion<string>().HasMaxLength(32);
                entity.Property(release => release.Version).HasMaxLength(40);
                entity.HasOne(release => release.Project)
                    .WithMany(project => project.Releases)
                    .HasForeignKey(release => release.GameProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            })
            .Entity<ReleaseGate>(entity => {
                entity.HasIndex(gate => new { gate.GameReleaseId, gate.Name }).IsUnique();
                entity.HasIndex(gate => new { gate.GameReleaseId, gate.SortOrder }).IsUnique();
                entity.Property(gate => gate.Status).HasConversion<string>().HasMaxLength(32);
                entity.Property(gate => gate.Name).HasMaxLength(80);
                entity.HasOne(gate => gate.Release)
                    .WithMany(release => release.Gates)
                    .HasForeignKey(gate => gate.GameReleaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            })
            .Entity<ReleaseCheckRun>(entity => {
                entity.HasIndex(run => new { run.Status, run.AttemptCount, run.QueuedAt });
                entity.Property(run => run.Status).HasConversion<string>().HasMaxLength(32);
                entity.Property(run => run.Kind).HasMaxLength(40);
                entity.HasOne(run => run.Gate)
                    .WithMany(gate => gate.CheckRuns)
                    .HasForeignKey(run => run.ReleaseGateId)
                    .OnDelete(DeleteBehavior.Cascade);
            })
            .Entity<BugReport>(entity => {
                entity.Property(bug => bug.Severity).HasConversion<int>();
                entity.Property(bug => bug.Status).HasConversion<string>().HasMaxLength(32);
                entity.HasOne(bug => bug.Release)
                    .WithMany(release => release.Bugs)
                    .HasForeignKey(bug => bug.GameReleaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            })
            .Entity<PlaytestFeedback>(entity => {
                entity.Property(feedback => feedback.Sentiment).HasConversion<string>().HasMaxLength(32);
                entity.HasOne(feedback => feedback.Project)
                    .WithMany(project => project.Feedback)
                    .HasForeignKey(feedback => feedback.GameProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
    }
}
