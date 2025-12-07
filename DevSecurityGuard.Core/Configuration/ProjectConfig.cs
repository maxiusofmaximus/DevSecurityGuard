using System.Text.Json;

namespace DevSecurityGuard.Core.Configuration;

/// <summary>
/// Project-level configuration (.devsecurityguard.json)
/// </summary>
public class ProjectConfig
{
    public string Version { get; set; } = "1.0";
    public bool Enabled { get; set; } = true;
    public string InterventionMode { get; set; } = "interactive";
    public string[] PackageManagers { get; set; } = Array.Empty<string>();
    public string[] Exclude { get; set; } = Array.Empty<string>();
    public Dictionary<string, DetectorConfig> Detectors { get; set; } = new();
    public NotificationConfig? Notifications { get; set; }
    public string[] Whitelist { get; set; } = Array.Empty<string>();
    public PerformanceConfig Performance { get; set; } = new();
    public PrivacyConfig Privacy { get; set; } = new();

    public static ProjectConfig Load(string projectPath)
    {
        var configPath = Path.Combine(projectPath, ".devsecurityguard.json");
        
        if (!File.Exists(configPath))
        {
            return GetDefault();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<ProjectConfig>(json) ?? GetDefault();
        }
        catch
        {
            return GetDefault();
        }
    }

    public static ProjectConfig GetDefault()
    {
        return new ProjectConfig
        {
            PackageManagers = new[] { "npm", "pip", "cargo" },
            Exclude = new[] { "node_modules/**", "venv/**", "*.test.js", "test/**" },
            Detectors = new Dictionary<string, DetectorConfig>
            {
                ["typosquatting"] = new() { Enabled = true, Threshold = 0.85 },
                ["ml-malware"] = new() { Enabled = true, Confidence = 0.75 },
                ["supplyChain"] = new() { Enabled = true }
            }
        };
    }

    public void Save(string projectPath)
    {
        var configPath = Path.Combine(projectPath, ".devsecurityguard.json");
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        File.WriteAllText(configPath, json);
    }
}

public class DetectorConfig
{
    public bool Enabled { get; set; } = true;
    public double? Threshold { get; set; }
    public double? Confidence { get; set; }
}

public class NotificationConfig
{
    public string? Slack { get; set; }
    public string? Email { get; set; }
    public string? Discord { get; set; }
}

public class PerformanceConfig
{
    public bool CacheEnabled { get; set; } = true;
    public int CacheTTL { get; set; } = 3600;
    public bool ParallelScans { get; set; } = true;
    public int MaxConcurrency { get; set; } = 4;
}

public class PrivacyConfig
{
    public bool TelemetryEnabled { get; set; } = false;
    public bool ThreatFeedEnabled { get; set; } = false;
    public bool EncryptDatabase { get; set; } = false;
}
