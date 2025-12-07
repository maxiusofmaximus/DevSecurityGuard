using DevSecurityGuard.Service.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DevSecurityGuard.Service.DetectionEngines;

/// <summary>
/// Specialized detector for Shai-Hulud worm and similar self-replicating npm malware
/// </summary>
public class ShaiHuludDetector : IThreatDetector
{
    private readonly ILogger<ShaiHuludDetector> _logger;
    private readonly HashSet<string> _knownMaliciousFiles;
    private readonly HashSet<string> _knownMaliciousRepoPatterns;

    public string DetectorName => "Shai-Hulud Specialist Detector";
    public int Priority => 100; // Highest priority - critical threat

    public ShaiHuludDetector(ILogger<ShaiHuludDetector> logger)
    {
        _logger = logger;
        _knownMaliciousFiles = InitializeKnownMaliciousFiles();
        _knownMaliciousRepoPatterns = InitializeRepoPatterns();
    }

    public async Task<ThreatDetectionResult> AnalyzePackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing {PackageName} for Shai-Hulud signatures", packageName);

        // Check package name against known compromised patterns
        if (IsKnownCompromisedPackage(packageName))
        {
            return ThreatDetectionResult.CreateThreat(
                ThreatType.ShaiHulud,
                ThreatSeverity.Critical,
                packageName,
                "Package name matches known Shai-Hulud compromised package database. DO NOT INSTALL.",
                version);
        }

        // In a full implementation, this would:
        // 1. Download package contents
        // 2. Scan for malicious files
        // 3. Check package.json for suspicious scripts
        // 4. Analyze for self-replication code

        return await Task.FromResult(ThreatDetectionResult.NoThreat(packageName, version));
    }

    /// <summary>
    /// Analyze package contents for Shai-Hulud signatures
    /// </summary>
    public ThreatDetectionResult AnalyzePackageContents(string packageName, IEnumerable<string> filePaths, string? packageJsonContent = null)
    {
        var threats = new List<string>();

        // Check for known malicious file names
        foreach (var filePath in filePaths)
        {
            var fileName = Path.GetFileName(filePath);
            if (_knownMaliciousFiles.Contains(fileName.ToLowerInvariant()))
            {
                threats.Add($"Contains known Shai-Hulud file: {fileName}");
            }
        }

        // Check for Bun runtime installation (Shai-Hulud indicator)
        if (filePaths.Any(f => f.Contains("setup_bun", StringComparison.OrdinalIgnoreCase) ||
                               f.Contains("bun_environment", StringComparison.OrdinalIgnoreCase)))
        {
            threats.Add("Attempts to install Bun runtime (Shai-Hulud evasion tactic)");
        }

        // Analyze package.json if provided
        if (packageJsonContent != null)
        {
            var scriptThreats = AnalyzePackageJsonForShaiHulud(packageJsonContent);
            threats.AddRange(scriptThreats);
        }

        if (threats.Count > 0)
        {
            return ThreatDetectionResult.CreateThreat(
                ThreatType.ShaiHulud,
                ThreatSeverity.Critical,
                packageName,
                $"CRITICAL: Shai-Hulud worm detected! {string.Join("; ", threats)}");
        }

        return ThreatDetectionResult.NoThreat(packageName);
    }

    /// <summary>
    /// Detect GitHub repository creation patterns associated with Shai-Hulud
    /// </summary>
    public bool IsShaiHuludRepositoryPattern(string repoName, string? description = null)
    {
        // Shai-Hulud creates repos with specific patterns
        foreach (var pattern in _knownMaliciousRepoPatterns)
        {
            if (Regex.IsMatch(repoName, pattern, RegexOptions.IgnoreCase))
            {
                _logger.LogCritical("Detected Shai-Hulud repository pattern: {RepoName}", repoName);
                return true;
            }
        }

        // Check description for known signatures
        if (description != null)
        {
            if (description.Contains("Shai-Hulud", StringComparison.OrdinalIgnoreCase) ||
                description.Contains("The Second Coming", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogCritical("Detected Shai-Hulud signature in repo description: {Description}", description);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detect processes associated with Shai-Hulud behavior
    /// </summary>
    public bool IsShaiHuludBehavior(string processName, string commandLine)
    {
        var indicators = new[]
        {
            "trufflehog",           // Secret scanning tool used by Shai-Hulud
            "gitleaks",             // Alternative secret scanner
            "gh api",               // GitHub API calls
            "git clone",            // Mass cloning behavior
            "bun install",          // Bun runtime installation
            "self-hosted runner",   // GitHub Actions runner registration
        };

        foreach (var indicator in indicators)
        {
            if (commandLine.Contains(indicator, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Detected Shai-Hulud behavioral indicator: {Indicator}", indicator);
                return true;
            }
        }

        return false;
    }

    private List<string> AnalyzePackageJsonForShaiHulud(string packageJsonContent)
    {
        var threats = new List<string>();

        // Check for preinstall script (Shai-Hulud v2 uses this)
        if (Regex.IsMatch(packageJsonContent, @"""preinstall""\s*:", RegexOptions.IgnoreCase))
        {
            threats.Add("Uses preinstall script (Shai-Hulud v2 indicator)");
        }

        // Check for trufflehog or secret scanning tools
        if (packageJsonContent.Contains("trufflehog", StringComparison.OrdinalIgnoreCase) ||
            packageJsonContent.Contains("gitleaks", StringComparison.OrdinalIgnoreCase))
        {
            threats.Add("References secret scanning tools (credential harvesting)");
        }

        // Check for GitHub API usage in scripts
        if (Regex.IsMatch(packageJsonContent, @"github\.com/api|api\.github\.com", RegexOptions.IgnoreCase))
        {
            threats.Add("Makes GitHub API calls (potential self-propagation)");
        }

        // Check for npm publish or version manipulation
        if (Regex.IsMatch(packageJsonContent, @"npm\s+publish|npm\s+version", RegexOptions.IgnoreCase))
        {
            threats.Add("Attempts to publish/version npm packages (self-replication)");
        }

        // Check for file deletion patterns (dead man's switch)
        if (Regex.IsMatch(packageJsonContent, @"rm\s+-rf\s+~|del\s+/s\s+/q|Remove-Item.*-Recurse", RegexOptions.IgnoreCase))
        {
            threats.Add("CRITICAL: Contains file deletion code (dead man's switch)");
        }

        return threats;
    }

    private bool IsKnownCompromisedPackage(string packageName)
    {
        // In production, this would query a live threat intelligence database
        // For now, we have a static list of known compromised packages

        var knownCompromisedPatterns = new string[]
        {
            // Add known compromised package patterns here
            // These would be updated from threat feeds
        };

        foreach (var pattern in knownCompromisedPatterns)
        {
            if (Regex.IsMatch(packageName, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private HashSet<string> InitializeKnownMaliciousFiles()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Shai-Hulud v1.0
            "shai-hulud-workflow.yml",
            "shai_hulud.js",
            
            // Shai-Hulud v2.0
            "setup_bun.js",
            "bun_environment.js",
            
            // General indicators
            "trufflehog",
            "git-secrets",
            "credential-harvester.js",
            ".shai-hulud",
        };
    }

    private HashSet<string> InitializeRepoPatterns()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"^[a-zA-Z0-9]{18}$",           // Random 18-character names (Shai-Hulud v2)
            @"^shai-?hulud",                 // Direct naming
            @"migration$",                   // -migration suffix
        };
    }
}
