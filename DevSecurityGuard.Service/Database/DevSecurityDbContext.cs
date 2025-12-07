using Microsoft.EntityFrameworkCore;
using DevSecurityGuard.Service.Models;

namespace DevSecurityGuard.Service.Database;

/// <summary>
/// Database context for DevSecurityGuard application
/// </summary>
public class DevSecurityDbContext : DbContext
{
    public DevSecurityDbContext(DbContextOptions<DevSecurityDbContext> options)
        : base(options)
    {
    }

    public DbSet<ThreatEntry> Threats { get; set; } = null!;
    public DbSet<WhitelistEntry> Whitelist { get; set; } = null!;
    public DbSet<BlacklistEntry> Blacklist { get; set; } = null!;
    public DbSet<ConfigurationEntry> Configuration { get; set; } = null!;
    public DbSet<ScanCache> ScanCache { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure indexes for better query performance
        modelBuilder.Entity<ThreatEntry>()
            .HasIndex(t => t.Timestamp);

        modelBuilder.Entity<ThreatEntry>()
            .HasIndex(t => t.PackageName);

        modelBuilder.Entity<WhitelistEntry>()
            .HasIndex(w => w.PackageName);

        modelBuilder.Entity<BlacklistEntry>()
            .HasIndex(b => b.PackageName);

        modelBuilder.Entity<ScanCache>()
            .HasIndex(s => new { s.PackageName, s.Version });

        modelBuilder.Entity<ScanCache>()
            .HasIndex(s => s.Expiry);

        // Seed default configuration
        modelBuilder.Entity<ConfigurationEntry>().HasData(
            new ConfigurationEntry
            {
                Key = "InterventionMode",
                Value = "Interactive",
                LastModified = DateTime.UtcNow
            },
            new ConfigurationEntry
            {
                Key = "MonitoredDirectories",
                Value = "[]",
                LastModified = DateTime.UtcNow
            },
            new ConfigurationEntry
            {
                Key = "ForcePnpm",
                Value = "true",
                LastModified = DateTime.UtcNow
            },
            new ConfigurationEntry
            {
                Key = "EnableEnvProtection",
                Value = "true",
                LastModified = DateTime.UtcNow
            },
            new ConfigurationEntry
            {
                Key = "EnableCredentialMonitoring",
                Value = "true",
                LastModified = DateTime.UtcNow
            }
        );
    }
}
