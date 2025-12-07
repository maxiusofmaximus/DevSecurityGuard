namespace DevSecurityGuard.Service.Models;

/// <summary>
/// Represents the intervention mode configuration
/// </summary>
public enum InterventionMode
{
    /// <summary>
    /// Automatically block threats without user prompts
    /// </summary>
    Automatic,

    /// <summary>
    /// Prompt user for each threat detected (default)
    /// </summary>
    Interactive,

    /// <summary>
    /// Only show alerts, do not block
    /// </summary>
    AlertOnly
}

/// <summary>
/// Severity level of detected threats
/// </summary>
public enum ThreatSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Type of threat detected
/// </summary>
public enum ThreatType
{
    Typosquatting,
    SupplyChainAttack,
    CredentialTheft,
    MaliciousScript,
    ShaiHulud,
    Unknown
}

/// <summary>
/// Action taken on a detected threat
/// </summary>
public enum ThreatAction
{
    Blocked,
    Allowed,
    Whitelisted,
    Quarantined,
    AlertOnly
}
