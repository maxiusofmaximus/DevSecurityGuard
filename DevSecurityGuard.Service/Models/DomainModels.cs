namespace DevSecurityGuard.Service.Models;

/// <summary>
/// Result of a threat detection operation
/// </summary>
public class ThreatDetectionResult
{
    public bool IsThreatDetected { get; set; }
    public ThreatType ThreatType { get; set; }
    public ThreatSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string? Version { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;

    public static ThreatDetectionResult NoThreat(string packageName, string? version = null)
    {
        return new ThreatDetectionResult
        {
            IsThreatDetected = false,
            PackageName = packageName,
            Version = version,
            Description = "No threats detected"
        };
    }

    public static ThreatDetectionResult CreateThreat(
        ThreatType type,
        ThreatSeverity severity,
        string packageName,
        string description,
        string? version = null)
    {
        return new ThreatDetectionResult
        {
            IsThreatDetected = true,
            ThreatType = type,
            Severity = severity,
            PackageName = packageName,
            Version = version,
            Description = description,
            RecommendedAction = severity >= ThreatSeverity.High ? "Block" : "Review"
        };
    }
}

/// <summary>
/// Configuration for the DevSecurityGuard service
/// </summary>
public class ServiceConfiguration
{
    public InterventionMode InterventionMode { get; set; } = InterventionMode.Interactive;
    public List<string> MonitoredDirectories { get; set; } = new();
    public bool ForcePnpm { get; set; } = true;
    public bool EnableEnvProtection { get; set; } = true;
    public bool EnableCredentialMonitoring { get; set; } = true;
    public int ScanCacheExpiryDays { get; set; } = 7;
    public int ThreatFeedUpdateIntervalHours { get; set; } = 6;
}
