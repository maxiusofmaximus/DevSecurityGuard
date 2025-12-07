namespace DevSecurityGuard.PluginSystem;

/// <summary>
/// Base interface for all plugins
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Plugin unique identifier (e.g., "community.typosquatting-detector")
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Plugin display name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version (semantic versioning)
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Plugin author/maintainer
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Initialize the plugin
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Cleanup resources
    /// </summary>
    Task DisposeAsync();
}

/// <summary>
/// Interface for detector plugins
/// </summary>
public interface IDetectorPlugin : IPlugin
{
    /// <summary>
    /// Package managers this detector supports
    /// </summary>
    string[] SupportedPackageManagers { get; }
    
    /// <summary>
    /// Priority (higher = runs first)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Analyze a package for threats
    /// </summary>
    Task<ThreatDetectionResult> AnalyzeAsync(PackageAnalysisContext context);
}

/// <summary>
/// Interface for package manager plugins
/// </summary>
public interface IPackageManagerPlugin : IPlugin
{
    /// <summary>
    /// Package manager name (npm, pip, cargo, etc.)
    /// </summary>
    string PackageManagerName { get; }
    
    /// <summary>
    /// Get the actual package manager implementation
    /// </summary>
    Core.Abstractions.IPackageManager GetPackageManager();
}

/// <summary>
/// Analysis context passed to detector plugins
/// </summary>
public class PackageAnalysisContext
{
    public string PackageName { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string PackageManager { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result from threat detection
/// </summary>
public class ThreatDetectionResult
{
    public bool ThreatDetected { get; set; }
    public string ThreatType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low"; // Low, Medium, High, Critical
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Details { get; set; } = new();
    public string DetectorId { get; set; } = string.Empty;
}
