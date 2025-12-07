using DevSecurityGuard.Service.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DevSecurityGuard.Service.DetectionEngines;

/// <summary>
/// Detects malicious scripts in package.json lifecycle hooks
/// </summary>
public class MaliciousScriptDetector : IThreatDetector
{
    private readonly ILogger<MaliciousScriptDetector> _logger;
    private readonly List<Regex> _suspiciousPatterns;
    private readonly HashSet<string> _suspiciousCommands;

    public string DetectorName => "Malicious Script Detector";
    public int Priority => 85;

    public MaliciousScriptDetector(ILogger<MaliciousScriptDetector> logger)
    {
        _logger = logger;
        _suspiciousPatterns = InitializeSuspiciousPatterns();
        _suspiciousCommands = InitializeSuspiciousCommands();
    }

    public async Task<ThreatDetectionResult> AnalyzePackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        // Note: In a full implementation, this would fetch and parse package.json
        // For now, this is a placeholder structure showing the detection logic
        _logger.LogDebug("Analyzing scripts for package {PackageName}", packageName);

        // TODO: Implement package.json fetching and parsing
        // For demonstration purposes, returning no threat
        return ThreatDetectionResult.NoThreat(packageName, version);
    }

    /// <summary>
    /// Analyzes script content for suspicious patterns
    /// </summary>
    public ThreatDetectionResult AnalyzeScriptContent(string packageName, string scriptContent, string scriptType = "postinstall")
    {
        _logger.LogDebug("Analyzing {ScriptType} script for {PackageName}", scriptType, packageName);

        var threats = new List<string>();

        // Check for suspicious commands
        foreach (var command in _suspiciousCommands)
        {
            if (scriptContent.Contains(command, StringComparison.OrdinalIgnoreCase))
            {
                threats.Add($"Suspicious command detected: {command}");
            }
        }

        // Check for pattern matches
        foreach (var pattern in _suspiciousPatterns)
        {
            if (pattern.IsMatch(scriptContent))
            {
                threats.Add($"Suspicious pattern detected: {pattern}");
            }
        }

        // Check for obfuscation indicators
        if (IsObfuscated(scriptContent))
        {
            threats.Add("Script appears to be heavily obfuscated");
        }

        if (threats.Count > 0)
        {
            var severity = threats.Count >= 3 ? ThreatSeverity.Critical : 
                          threats.Count == 2 ? ThreatSeverity.High : ThreatSeverity.Medium;

            return ThreatDetectionResult.CreateThreat(
                ThreatType.MaliciousScript,
                severity,
                packageName,
                $"Malicious script detected in {scriptType}: " + string.Join("; ", threats));
        }

        return ThreatDetectionResult.NoThreat(packageName);
    }

    private bool IsObfuscated(string scriptContent)
    {
        int obfuscationScore = 0;

        // Check for excessive use of eval
        if (Regex.Matches(scriptContent, @"\beval\s*\(").Count > 2)
            obfuscationScore += 2;

        // Long base64 strings
        if (Regex.IsMatch(scriptContent, @"[A-Za-z0-9+/]{100,}={0,2}"))
            obfuscationScore += 2;

        // Excessive hex encoding
        if (Regex.Matches(scriptContent, @"\\x[0-9a-fA-F]{2}").Count > 20)
            obfuscationScore += 2;

        // Unicode escapes
        if (Regex.Matches(scriptContent, @"\\u[0-9a-fA-F]{4}").Count > 20)
            obfuscationScore += 1;

        // Very long single lines (minified/obfuscated code)
        var lines = scriptContent.Split('\n');
        if (lines.Any(line => line.Length > 500))
            obfuscationScore += 1;

        return obfuscationScore >= 3;
    }

    private List<Regex> InitializeSuspiciousPatterns()
    {
        return new List<Regex>
        {
            // Network requests to IPs or suspicious domains
            new Regex(@"(curl|wget|fetch|axios\.get|https?\.request).*?(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.IgnoreCase),
            
            // Pipe to bash/sh
            new Regex(@"\|\s*(bash|sh|zsh|fish|pwsh)", RegexOptions.IgnoreCase),
            
            // File deletion patterns
            new Regex(@"rm\s+-rf\s+[~/]", RegexOptions.IgnoreCase),
            
            // eval with encoded content
            new Regex(@"eval\s*\(\s*.*?(atob|Buffer\.from|decodeURI)"),
            
            // Downloading executables
            new Regex(@"(curl|wget).*?\.(exe|sh|bat|ps1|dll)", RegexOptions.IgnoreCase),
            
            // Blockchain/crypto mining patterns
            new Regex(@"(stratum|xmr|monero|mining|cryptonight)", RegexOptions.IgnoreCase),
            
            // Credential harvesting tools
            new Regex(@"(trufflehog|gitleaks|git-secrets)", RegexOptions.IgnoreCase),
            
            // Hidden/background processes
            new Regex(@"(nohup|disown|\&\s*$|start\s+/b)", RegexOptions.IgnoreCase)
        };
    }

    private HashSet<string> InitializeSuspiciousCommands()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Network commands
            "curl", "wget", "nc", "netcat", "telnet",
            
            // Code execution
            "eval(", "Function(", "setTimeout(", "setInterval(",
            "vm.runInNewContext", "vm.runInThisContext",
            
            // File operations
            "/dev/null", "rm -rf", "del /f", "Remove-Item -Recurse",
            
            // Environment variable access
            "process.env", "$env:", "os.environ",
            
            // Binary execution
            "exec(", "spawn(", "execSync(", "spawnSync(",
            "child_process", "ShellExecute",
            
            // Credential access
            ".aws/credentials", ".npmrc", ".gitconfig", ".ssh/",
            "git-credentials", "credential-helper",
            
            // Crypto/mining
            "cpuminer", "xmrig", "minerd",
            
            // Obfuscation
            "atob(", "btoa(", "Buffer.from", "toString('base64')",
            
            // Dangerous PowerShell
            "Invoke-Expression", "IEX", "Invoke-WebRequest Download"
        };
    }
}
