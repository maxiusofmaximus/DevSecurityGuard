using DevSecurityGuard.Service.DetectionEngines;
using DevSecurityGuard.Service.Models;
using Microsoft.Extensions.Logging;

namespace DevSecurityGuard.Service;

/// <summary>
/// Intercepts and validates package manager commands
/// </summary>
public class PackageManagerInterceptor
{
    private readonly ILogger<PackageManagerInterceptor> _logger;
    private readonly IEnumerable<IThreatDetector> _threatDetectors;
    private readonly ServiceConfiguration _configuration;

    public PackageManagerInterceptor(
        ILogger<PackageManagerInterceptor> logger,
        IEnumerable<IThreatDetector> threatDetectors,
        ServiceConfiguration configuration)
    {
        _logger = logger;
        _threatDetectors = threatDetectors.OrderByDescending(d => d.Priority);
        _configuration = configuration;
    }

    /// <summary>
    /// Analyze a package installation command
    /// </summary>
    public async Task<PackageInterceptionResult> AnalyzeInstallationAsync(
        string packageManager,
        string command,
        List<string> packageNames,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Intercepted {PackageManager} command: {Command}", packageManager, command);

        var result = new PackageInterceptionResult
        {
            OriginalCommand = command,
            PackageManager = packageManager,
            PackageNames = packageNames,
            ShouldBlock = false
        };

        // Analyze each package
        foreach (var packageName in packageNames)
        {
            var threats = await AnalyzePackageAsync(packageName, null, cancellationToken);
            
            if (threats.Any(t => t.IsThreatDetected))
            {
                result.DetectedThreats.AddRange(threats.Where(t => t.IsThreatDetected));

                // Determine if we should block based on severity
                var hasHighSeverity = threats.Any(t => 
                    t.Severity >= ThreatSeverity.High && t.IsThreatDetected);

                if (hasHighSeverity)
                {
                    result.ShouldBlock = true;
                    result.BlockReason = $"High severity threat detected in package '{packageName}'";
                    _logger.LogWarning("Blocking installation of {PackageName} due to high severity threat", packageName);
                }
            }
        }

        // Check if we should redirect to pnpm
        if (_configuration.ForcePnpm && packageManager != "pnpm")
        {
            result.ShouldRedirect = true;
            result.RedirectedCommand = ConvertToPnpmCommand(command, packageManager);
            _logger.LogInformation("Redirecting {PackageManager} to pnpm", packageManager);
        }

        return result;
    }

    /// <summary>
    /// Parse package manager command to extract package names
    /// </summary>
    public static PackageCommandInfo ParseCommand(string fullCommand)
    {
        var parts = fullCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
            return new PackageCommandInfo();

        var info = new PackageCommandInfo
        {
            PackageManager = parts[0].ToLowerInvariant()
        };

        // Remove file extension if present
        if (info.PackageManager.EndsWith(".cmd") || info.PackageManager.EndsWith(".exe"))
        {
            info.PackageManager = Path.GetFileNameWithoutExtension(info.PackageManager);
        }

        // Find the subcommand (install, add, etc.)
        var subcommandIndex = Array.FindIndex(parts, 1, p => 
            !p.StartsWith("-") && IsSubcommand(p));

        if (subcommandIndex > 0)
        {
            info.Subcommand = parts[subcommandIndex];

            // Extract package names (everything after subcommand that doesn't start with -)
            for (int i = subcommandIndex + 1; i < parts.Length; i++)
            {
                if (!parts[i].StartsWith("-"))
                {
                    info.PackageNames.Add(parts[i]);
                }
                else
                {
                    info.Flags.Add(parts[i]);
                }
            }
        }

        return info;
    }

    private static bool IsSubcommand(string word)
    {
        var subcommands = new[] { "install", "i", "add", "update", "upgrade", "ci" };
        return subcommands.Contains(word.ToLowerInvariant());
    }

    private string ConvertToPnpmCommand(string originalCommand, string originalPackageManager)
    {
        var command = originalCommand;

        // Replace package manager
        if (originalPackageManager == "npm")
        {
            command = command.Replace("npm install", "pnpm add");
            command = command.Replace("npm i ", "pnpm add ");
            command = command.Replace("npm ci", "pnpm install --frozen-lockfile");
        }
        else if (originalPackageManager == "yarn")
        {
            command = command.Replace("yarn add", "pnpm add");
            command = command.Replace("yarn install", "pnpm install");
        }

        return command;
    }

    private async Task<List<ThreatDetectionResult>> AnalyzePackageAsync(
        string packageName,
        string? version,
        CancellationToken cancellationToken)
    {
        var results = new List<ThreatDetectionResult>();

        foreach (var detector in _threatDetectors)
        {
            try
            {
                var result = await detector.AnalyzePackageAsync(packageName, version, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running detector {Detector} for package {PackageName}",
                    detector.DetectorName, packageName);
            }
        }

        return results;
    }
}

/// <summary>
/// Result of package interception and analysis
/// </summary>
public class PackageInterceptionResult
{
    public string OriginalCommand { get; set; } = string.Empty;
    public string PackageManager { get; set; } = string.Empty;
    public List<string> PackageNames { get; set; } = new();
    public List<ThreatDetectionResult> DetectedThreats { get; set; } = new();
    public bool ShouldBlock { get; set; }
    public string? BlockReason { get; set; }
    public bool ShouldRedirect { get; set; }
    public string? RedirectedCommand { get; set; }

    public bool HasThreats => DetectedThreats.Any(t => t.IsThreatDetected);
    public int ThreatCount => DetectedThreats.Count(t => t.IsThreatDetected);
}

/// <summary>
/// Information parsed from package manager command
/// </summary>
public class PackageCommandInfo
{
    public string PackageManager { get; set; } = string.Empty;
    public string Subcommand { get; set; } = string.Empty;
    public List<string> PackageNames { get; set; } = new();
    public List<string> Flags { get; set; } = new();
}
