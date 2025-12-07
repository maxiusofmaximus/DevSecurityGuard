using DevSecurityGuard.Service;
using DevSecurityGuard.Service.Database;
using DevSecurityGuard.Service.DetectionEngines;
using DevSecurityGuard.Service.Models;
using DevSecurityGuard.Core.Abstractions;
using DevSecurityGuard.Core.PackageManagers;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/devsecurityguard-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("DevSecurityGuard Service starting");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "DevSecurityGuard";
    });

    // Configure Serilog
    builder.Services.AddSerilog();

    // Configure Database
    var dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DevSecurityGuard",
        "devsecurity.db");

    // Ensure directory exists
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

    // Configure dependency injection
    builder.Services.AddHttpClient();

    // Register all 8 package managers
    builder.Services.AddSingleton<IPackageManager, NpmPackageManager>();
    builder.Services.AddSingleton<IPackageManager, PipPackageManager>();
    builder.Services.AddSingleton<IPackageManager, CargoPackageManager>();
    builder.Services.AddSingleton<IPackageManager, NuGetPackageManager>();
    builder.Services.AddSingleton<IPackageManager, MavenPackageManager>();
    builder.Services.AddSingleton<IPackageManager, GradlePackageManager>();
    builder.Services.AddSingleton<IPackageManager, GemPackageManager>();
    builder.Services.AddSingleton<IPackageManager, ComposerPackageManager>();

    // Register package manager factory
    builder.Services.AddSingleton<IPackageManagerFactory, PackageManagerFactory>();

    // Database
    builder.Services.AddDbContext<DevSecurityDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

    // Register threat detectors in priority order
    builder.Services.AddSingleton<IThreatDetector, ShaiHuludDetector>();
    builder.Services.AddSingleton<IThreatDetector, CredentialTheftDetector>();
    builder.Services.AddSingleton<IThreatDetector, TyposquattingDetector>();
    builder.Services.AddSingleton<IThreatDetector, MaliciousScriptDetector>();
    builder.Services.AddSingleton<IThreatDetector, SupplyChainDetector>();

    // Register service configuration
    builder.Services.AddSingleton(new ServiceConfiguration());

    // Register intervention system
    builder.Services.AddSingleton<PackageManagerInterceptor>();
    builder.Services.AddSingleton<ProcessMonitor>();

    // Register main worker service
    builder.Services.AddHostedService<DevSecurityWorker>();

    var host = builder.Build();

    // Initialize database on first run
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DevSecurityDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        Log.Information("Database initialized at: {DbPath}", dbPath);
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
