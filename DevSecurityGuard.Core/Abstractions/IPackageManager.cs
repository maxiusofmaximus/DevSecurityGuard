namespace DevSecurityGuard.Core.Abstractions;

/// <summary>
/// Base interface for all package managers
/// </summary>
public interface IPackageManager
{
    /// <summary>
    /// Package manager name (npm, pip, cargo, etc.)
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Package manager display name
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// File patterns to detect this package manager
    /// Example: package.json for npm, requirements.txt for pip
    /// </summary>
    string[] DetectionPatterns { get; }
    
    /// <summary>
    /// Lock file patterns (for dependency resolution)
    /// Example: package-lock.json, yarn.lock, Pipfile.lock
    /// </summary>
    string[] LockFilePatterns { get; }
    
    /// <summary>
    /// Detect if this package manager is used in the given directory
    /// </summary>
    bool IsDetected(string projectPath);
    
    /// <summary>
    /// Get the registry URL for this package manager
    /// </summary>
    string GetRegistryUrl();
    
    /// <summary>
    /// Parse package information from manifest file
    /// </summary>
    Task<PackageManifest> ParseManifestAsync(string manifestPath);
    
    /// <summary>
    /// Parse lock file to get exact dependency versions
    /// </summary>
    Task<IEnumerable<PackageDependency>> ParseLockFileAsync(string lockFilePath);
    
    /// <summary>
    /// Get package metadata from registry
    /// </summary>
    Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null);
    
    /// <summary>
    /// Check if a package name matches typosquatting patterns
    /// </summary>
    Task<IEnumerable<string>> GetPopularPackagesAsync();
}

/// <summary>
/// Package manifest information
/// </summary>
public class PackageManifest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string> Dependencies { get; set; } = new();
    public Dictionary<string, string> DevDependencies { get; set; } = new();
    public Dictionary<string, string>? Scripts { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? License { get; set; }
}

/// <summary>
/// Package dependency information
/// </summary>
public class PackageDependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? ResolvedVersion { get; set; }
    public bool IsDev { get; set; }
    public string Source { get; set; } = string.Empty; // registry, git, file, etc.
}

/// <summary>
/// Package metadata from registry
/// </summary>
public class PackageMetadata
{
    public string Name { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? License { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int DownloadCount { get; set; }
    public string[] Maintainers { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Versions { get; set; } = new();
    public string? Homepage { get; set; }
    public string? Repository { get; set; }
}
