using DevSecurityGuard.Service.Database;
using DevSecurityGuard.Service.DetectionEngines;
using DevSecurityGuard.Service.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevSecurityGuard.Service;

/// <summary>
/// Main Windows Service worker for DevSecurityGuard
/// </summary>
public class DevSecurityWorker : BackgroundService
{
    private readonly ILogger<DevSecurityWorker> _logger;
    private readonly DevSecurityDbContext _dbContext;
    private readonly IEnumerable<IThreatDetector> _threatDetectors;
    private ServiceConfiguration _configuration;

    public DevSecurityWorker(
        ILogger<DevSecurityWorker> logger,
        DevSecurityDbContext dbContext,
        IEnumerable<IThreatDetector> threatDetectors)
    {
        _logger = logger;
        _dbContext = dbContext;
        _threatDetectors = threatDetectors.OrderByDescending(d => d.Priority);
        _configuration = new ServiceConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DevSecurityGuard Service starting at: {time}", DateTimeOffset.Now);

        try
        {
            // Ensure database is created
            await _dbContext.Database.EnsureCreatedAsync(stoppingToken);
            _logger.LogInformation("Database initialized successfully");

            // Load configuration
            await LoadConfigurationAsync(stoppingToken);
            _logger.LogInformation("Configuration loaded. Intervention mode: {Mode}", _configuration.InterventionMode);

            // Start monitoring
            await StartMonitoringAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error starting DevSecurityGuard service");
            throw;
        }
    }

    private async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var interventionModeConfig = await _dbContext.Configuration
                .FindAsync(new object[] { "InterventionMode" }, cancellationToken);

            if (interventionModeConfig?.Value != null &&
                Enum.TryParse<InterventionMode>(interventionModeConfig.Value, out var mode))
            {
                _configuration.InterventionMode = mode;
            }

            var forcePnpmConfig = await _dbContext.Configuration
                .FindAsync(new object[] { "ForcePnpm" }, cancellationToken);

            if (forcePnpmConfig?.Value != null &&
                bool.TryParse(forcePnpmConfig.Value, out var forcePnpm))
            {
                _configuration.ForcePnpm = forcePnpm;
            }

            _logger.LogInformation("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading configuration, using defaults");
        }
    }

    private async Task StartMonitoringAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting threat monitoring with {DetectorCount} detectors", _threatDetectors.Count());

        foreach (var detector in _threatDetectors)
        {
            _logger.LogInformation("Loaded detector: {DetectorName} (Priority: {Priority})",
                detector.DetectorName, detector.Priority);
        }

        // Main service loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Periodic tasks
                await PerformPeriodicTasksAsync(stoppingToken);

                // Wait before next check
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task PerformPeriodicTasksAsync(CancellationToken cancellationToken)
    {
        // Clean up expired scan cache
        var expired = DateTime.UtcNow;
        var expiredScans = _dbContext.ScanCache.Where(s => s.Expiry < expired);

        if (await expiredScans.AnyAsync(cancellationToken))
        {
            _dbContext.ScanCache.RemoveRange(expiredScans);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up expired scan cache entries");
        }

        // TODO: Update threat intelligence feeds
        // TODO: Check for service updates
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DevSecurityGuard Service stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Analyze a package using all registered threat detectors
    /// </summary>
    public async Task<List<ThreatDetectionResult>> AnalyzePackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing package: {PackageName} {Version}", packageName, version ?? "latest");

        var results = new List<ThreatDetectionResult>();

        foreach (var detector in _threatDetectors)
        {
            try
            {
                var result = await detector.AnalyzePackageAsync(packageName, version, cancellationToken);
                if (result.IsThreatDetected)
                {
                    results.Add(result);
                    _logger.LogWarning("Threat detected by {Detector}: {Description}",
                        detector.DetectorName, result.Description);

                    // Log to database
                    await LogThreatAsync(result, ThreatAction.AlertOnly, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running detector {Detector}", detector.DetectorName);
            }
        }

        return results;
    }

    private async Task LogThreatAsync(
        ThreatDetectionResult threat,
        ThreatAction action,
        CancellationToken cancellationToken)
    {
        var entry = new ThreatEntry
        {
            Timestamp = DateTime.UtcNow,
            PackageName = threat.PackageName,
            Version = threat.Version,
            ThreatType = threat.ThreatType,
            Severity = threat.Severity,
            Description = threat.Description,
            ActionTaken = action,
            AdditionalInfo = string.Join("; ", threat.AdditionalData.Select(kvp => $"{kvp.Key}={kvp.Value}"))
        };

        _dbContext.Threats.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
