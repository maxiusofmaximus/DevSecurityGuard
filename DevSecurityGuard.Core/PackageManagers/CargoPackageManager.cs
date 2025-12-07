using System.Text.Json;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// Rust Cargo package manager implementation
/// </summary>
public class CargoPackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "cargo";
    public string DisplayName => "Cargo (Rust)";
    
    public string[] DetectionPatterns => new[]
    {
        "Cargo.toml"
    };
    
    public string[] LockFilePatterns => new[]
    {
        "Cargo.lock"
    };
    
    public CargoPackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return File.Exists(Path.Combine(projectPath, "Cargo.toml"));
    }
    
    public string GetRegistryUrl()
    {
        return "https://crates.io/api/v1";
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        // TODO: Implement proper TOML parsing
        // For now, simple regex-based parsing
        var content = await File.ReadAllTextAsync(manifestPath);
        var lines = content.Split('\n');
        
        bool inDependencies = false;
        bool inDevDependencies = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("[dependencies]"))
            {
                inDependencies = true;
                inDevDependencies = false;
                continue;
            }
            else if (trimmed.StartsWith("[dev-dependencies]"))
            {
                inDependencies = false;
                inDevDependencies = true;
                continue;
            }
            else if (trimmed.StartsWith('['))
            {
                inDependencies = false;
                inDevDependencies = false;
                continue;
            }
            
            if ((inDependencies || inDevDependencies) && trimmed.Contains('='))
            {
                var parts = trimmed.Split('=', 2);
                if (parts.Length == 2)
                {
                    var packageName = parts[0].Trim();
                    var version = parts[1].Trim().Trim('"', '\'', ' ');
                    
                    if (inDependencies)
                    {
                        manifest.Dependencies[packageName] = version;
                    }
                    else
                    {
                        manifest.DevDependencies[packageName] = version;
                    }
                }
            }
        }
        
        return manifest;
    }
    
    public async Task<IEnumerable<PackageDependency>> ParseLockFileAsync(string lockFilePath)
    {
        var dependencies = new List<PackageDependency>();
        
        // TODO: Implement proper Cargo.lock parsing (TOML format)
        // Cargo.lock format is complex, would need TOML parser
        
        return dependencies;
    }
    
    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        try
        {
            var url = $"{GetRegistryUrl()}/crates/{packageName}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var crateData = JsonSerializer.Deserialize<CratesIoResponse>(json);
            
            if (crateData?.Crate == null)
                return new PackageMetadata { Name = packageName };
            
            return new PackageMetadata
            {
                Name = crateData.Crate.Name ?? packageName,
                LatestVersion = crateData.Crate.NewestVersion ?? "unknown",
                Description = crateData.Crate.Description,
                License = crateData.Crate.License,
                DownloadCount = crateData.Crate.Downloads,
                Repository = crateData.Crate.Repository,
                Homepage = crateData.Crate.Homepage
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Top popular Rust crates
        return new[]
        {
            "serde", "tokio", "rand", "clap", "regex",
            "syn", "quote", "proc-macro2", "libc", "log",
            "futures", "async-trait", "thiserror", "anyhow", "chrono",
            "serde_json", "reqwest", "uuid", "lazy_static", "bitflags",
            "num-traits", "itertools", "rayon", "parking_lot", "crossbeam",
            "hyper", "tracing", "diesel", "actix-web", "rocket"
        };
    }
}

// Crates.io API response models
internal class CratesIoResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("crate")]
    public CrateInfo? Crate { get; set; }
}

internal class CrateInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("newest_version")]
    public string? NewestVersion { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("license")]
    public string? License { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("downloads")]
    public int Downloads { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("repository")]
    public string? Repository { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("homepage")]
    public string? Homepage { get; set; }
}
