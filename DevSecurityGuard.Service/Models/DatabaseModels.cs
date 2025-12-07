using System.ComponentModel.DataAnnotations;

namespace DevSecurityGuard.Service.Models;

/// <summary>
/// Represents a detected threat entry in the database
/// </summary>
public class ThreatEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    [MaxLength(500)]
    public string PackageName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Version { get; set; }

    [Required]
    public ThreatType ThreatType { get; set; }

    [Required]
    public ThreatSeverity Severity { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public ThreatAction ActionTaken { get; set; }

    [MaxLength(1000)]
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Represents a whitelisted package
/// </summary>
public class WhitelistEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string PackageName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AddedBy { get; set; }

    [Required]
    public DateTime AddedDate { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Represents a blacklisted package
/// </summary>
public class BlacklistEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string PackageName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = "User";

    [Required]
    public DateTime AddedDate { get; set; }

    [MaxLength(1000)]
    public string? ThreatInfo { get; set; }
}

/// <summary>
/// Configuration settings stored in database
/// </summary>
public class ConfigurationEntry
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Value { get; set; }

    public DateTime LastModified { get; set; }
}

/// <summary>
/// Cache for package scan results
/// </summary>
public class ScanCache
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string PackageName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;

    [Required]
    public DateTime LastScanned { get; set; }

    [Required]
    [MaxLength(50)]
    public string Result { get; set; } = string.Empty;

    [Required]
    public DateTime Expiry { get; set; }

    [MaxLength(2000)]
    public string? ScanDetails { get; set; }
}
