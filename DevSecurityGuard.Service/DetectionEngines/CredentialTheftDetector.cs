using DevSecurityGuard.Service.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DevSecurityGuard.Service.DetectionEngines;

/// <summary>
/// Detects credential theft attempts by monitoring sensitive file access
/// </summary>
public class CredentialTheftDetector : IThreatDetector
{
    private readonly ILogger<CredentialTheftDetector> _logger;
    private readonly HashSet<string> _sensitiveFiles;
    private readonly HashSet<string> _sensitiveDirectories;

    public string DetectorName => "Credential Theft Detector";
    public int Priority => 95; // Very high priority

    public CredentialTheftDetector(ILogger<CredentialTheftDetector> logger)
    {
        _logger = logger;
        _sensitiveFiles = InitializeSensitiveFiles();
        _sensitiveDirectories = InitializeSensitiveDirectories();
    }

    public async Task<ThreatDetectionResult> AnalyzePackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        // This detector works better as a real-time file monitor
        // For package analysis, we return no threat (file monitoring happens elsewhere)
        _logger.LogDebug("Credential theft detector - package analysis not applicable for {PackageName}", packageName);
        return await Task.FromResult(ThreatDetectionResult.NoThreat(packageName, version));
    }

    /// <summary>
    /// Check if a file path is sensitive and should be protected
    /// </summary>
    public bool IsSensitiveFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? string.Empty;

        // Check for exact sensitive filenames
        if (_sensitiveFiles.Contains(fileName))
        {
            _logger.LogWarning("Access to sensitive file detected: {FilePath}", filePath);
            return true;
        }

        // Check for sensitive directories
        foreach (var sensitiveDir in _sensitiveDirectories)
        {
            if (directory.Contains(sensitiveDir))
            {
                _logger.LogWarning("Access to sensitive directory detected: {FilePath}", filePath);
                return true;
            }
        }

        // Check for patterns
        if (IsSensitivePattern(fileName))
        {
            _logger.LogWarning("Access to file matching sensitive pattern: {FilePath}", filePath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Analyze a process accessing a sensitive file
    /// </summary>
    public ThreatDetectionResult AnalyzeFileAccess(string filePath, string processName, string? packageContext = null)
    {
        if (!IsSensitiveFile(filePath))
        {
            return ThreatDetectionResult.NoThreat(packageContext ?? "Unknown");
        }

        // Check if the process is suspicious
        var isSuspicious = IsSuspiciousProcess(processName);
        var severity = DetermineSeverity(filePath, processName);

        if (isSuspicious)
        {
            return ThreatDetectionResult.CreateThreat(
                ThreatType.CredentialTheft,
                severity,
                packageContext ?? processName,
                $"Suspicious process '{processName}' attempted to access sensitive file: {filePath}");
        }

        // Even if not suspicious, log the access
        _logger.LogInformation("Process {ProcessName} accessed sensitive file {FilePath}", processName, filePath);

        return ThreatDetectionResult.CreateThreat(
            ThreatType.CredentialTheft,
            ThreatSeverity.Medium,
            packageContext ?? processName,
            $"Process '{processName}' accessed sensitive file: {filePath}. Verify this is expected behavior.");
    }

    private bool IsSensitivePattern(string fileName)
    {
        // Patterns for sensitive files
        var patterns = new[]
        {
            @"\.env(\..+)?$",           // .env, .env.local, etc.
            @"\.aws",                    // AWS config
            @"\.azure",                  // Azure config
            @"credentials?",             // credentials, credential
            @"\.npmrc",                  // npm config
            @"\.gitconfig",              // Git config
            @"\.ssh",                    // SSH keys
            @"id_rsa",                   // SSH private key
            @"id_ed25519",               // SSH private key
            @"\.pem$",                   // Certificate files
            @"\.key$",                   // Key files
            @"\.p12$",                   // Certificate files
            @"\.pfx$",                   // Certificate files
            @"secrets?",                 // secrets, secret
            @"api[_-]?key",              // API keys
            @"access[_-]?token",         // Access tokens
            @"auth[_-]?token",           // Auth tokens
        };

        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSuspiciousProcess(string processName)
    {
        var suspiciousProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node.exe",          // Node.js (when run from postinstall)
            "bun.exe",           // Bun runtime (Shai-Hulud indicator)
            "curl.exe",          
            "wget.exe",
            "powershell.exe",    // Unless from authorized scripts
            "cmd.exe",
            "bash.exe",
            "sh.exe",
            "python.exe",        // Unless explicitly authorized
            "pythonw.exe",
        };

        var processBaseName = Path.GetFileNameWithoutExtension(processName).ToLowerInvariant();
        return suspiciousProcesses.Contains(processBaseName + ".exe");
    }

    private ThreatSeverity DetermineSeverity(string filePath, string processName)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();

        // Critical severity for SSH keys and cloud credentials
        if (fileName.Contains("id_rsa") || fileName.Contains("id_ed25519") ||
            filePath.Contains(".aws") || filePath.Contains(".azure") || filePath.Contains(".ssh"))
        {
            return ThreatSeverity.Critical;
        }

        // High severity for .env files and API tokens
        if (fileName.StartsWith(".env") || fileName.Contains("token") || fileName.Contains("secret"))
        {
            return ThreatSeverity.High;
        }

        // Medium for other sensitive files
        return ThreatSeverity.Medium;
    }

    private HashSet<string> InitializeSensitiveFiles()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Environment files
            ".env",
            ".env.local",
            ".env.development",
            ".env.production",
            ".env.test",
            
            // npm/yarn/pnpm
            ".npmrc",
            ".yarnrc",
            ".yarnrc.yml",
            ".pnpmfile.cjs",
            
            // Git
            ".gitconfig",
            ".git-credentials",
            "git-credentials",
            
            // SSH
            "id_rsa",
            "id_dsa",
            "id_ecdsa",
            "id_ed25519",
            "known_hosts",
            "authorized_keys",
            
            // Cloud providers
            "credentials",
            "config",
            
            // Certificates
            "*.pem",
            "*.key",
            "*.p12",
            "*.pfx",
            
            // Database
            ".pgpass",
            ".my.cnf",
            
            // Other
            "secrets.json",
            "appsettings.secrets.json",
        };
    }

    private HashSet<string> InitializeSensitiveDirectories()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".ssh",
            ".aws",
            ".azure",
            ".config\\gcloud",
            ".kube",
            ".docker",
            "google cloud",
        };
    }
}
