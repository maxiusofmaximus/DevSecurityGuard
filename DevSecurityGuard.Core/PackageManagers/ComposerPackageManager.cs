using System.Text.Json;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// Composer (PHP) package manager implementation
/// </summary>
public class ComposerPackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "composer";
    public string DisplayName => "Composer (PHP)";
    
    public string[] DetectionPatterns => new[]
    {
        "composer.json"
    };
    
    public string[] LockFilePatterns => new[]
    {
        "composer.lock"
    };
    
    public ComposerPackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return File.Exists(Path.Combine(projectPath, "composer.json"));
    }
    
    public string GetRegistryUrl()
    {
        return "https://packagist.org";
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            var composerData = JsonSerializer.Deserialize<ComposerJson>(json);
            
            if (composerData == null)
                return manifest;
            
            manifest.Name = composerData.Name ?? "";
            manifest.Version = composerData.Version ?? "";
            manifest.Description = composerData.Description;
            manifest.License = composerData.License?.FirstOrDefault();
            
            // Parse dependencies
            if (composerData.Require != null)
            {
                foreach (var (name, version) in composerData.Require)
                {
                    // Skip PHP platform requirements
                    if (name != "php" && !name.StartsWith("ext-"))
                    {
                        manifest.Dependencies[name] = version;
                    }
                }
            }
            
            // Parse dev dependencies
            if (composerData.RequireDev != null)
            {
                foreach (var (name, version) in composerData.RequireDev)
                {
                    manifest.DevDependencies[name] = version;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return manifest;
    }
    
    public async Task<IEnumerable<PackageDependency>> ParseLockFileAsync(string lockFilePath)
    {
        var dependencies = new List<PackageDependency>();
        
        try
        {
            var json = await File.ReadAllTextAsync(lockFilePath);
            var lockData = JsonSerializer.Deserialize<ComposerLock>(json);
            
            if (lockData?.Packages != null)
            {
                foreach (var pkg in lockData.Packages)
                {
                    dependencies.Add(new PackageDependency
                    {
                        Name = pkg.Name ?? "",
                        Version = pkg.Version ?? "*",
                        ResolvedVersion = pkg.Version,
                        IsDev = false,
                        Source = pkg.Source?.Type ?? "packagist"
                    });
                }
            }
            
            if (lockData?.PackagesDev != null)
            {
                foreach (var pkg in lockData.PackagesDev)
                {
                    dependencies.Add(new PackageDependency
                    {
                        Name = pkg.Name ?? "",
                        Version = pkg.Version ?? "*",
                        ResolvedVersion = pkg.Version,
                        IsDev = true,
                        Source = pkg.Source?.Type ?? "packagist"
                    });
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return dependencies;
    }
    
    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        try
        {
            var url = $"{GetRegistryUrl()}/packages/{packageName}.json";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var packageData = JsonSerializer.Deserialize<PackagistResponse>(json);
            
            if (packageData?.Package == null)
                return new PackageMetadata { Name = packageName };
            
            var versions = packageData.Package.Versions?.Values.FirstOrDefault();
            
            return new PackageMetadata
            {
                Name = packageName,
                LatestVersion = versions?.Version ?? "unknown",
                Description = versions?.Description,
                License = versions?.License?.FirstOrDefault(),
                Homepage = versions?.Homepage,
                Repository = versions?.Source?.Url,
                DownloadCount = packageData.Package.Downloads?.Total ?? 0
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Popular Composer packages
        return new[]
        {
            "symfony/symfony", "laravel/framework", "guzzlehttp/guzzle",
            "monolog/monolog", "phpunit/phpunit", "doctrine/orm",
            "symfony/console", "symfony/http-foundation", "psr/log",
            "vlucas/phpdotenv", "league/flysystem", "nesbot/carbon",
            "intervention/image", "predis/predis", "swiftmailer/swiftmailer"
        };
    }
}

// Composer JSON models
internal class ComposerJson
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public List<string>? License { get; set; }
    public Dictionary<string, string>? Require { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("require-dev")]
    public Dictionary<string, string>? RequireDev { get; set; }
}

internal class ComposerLock
{
    public List<ComposerPackage>? Packages { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("packages-dev")]
    public List<ComposerPackage>? PackagesDev { get; set; }
}

internal class ComposerPackage
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public ComposerSource? Source { get; set; }
}

internal class ComposerSource
{
    public string? Type { get; set; }
    public string? Url { get; set; }
}

internal class PackagistResponse
{
    public PackagistPackage? Package { get; set; }
}

internal class PackagistPackage
{
    public Dictionary<string, PackagistVersion>? Versions { get; set; }
    public PackagistDownloads? Downloads { get; set; }
}

internal class PackagistVersion
{
    public string? Version { get; set; }
    public string? Description { get; set; }
    public List<string>? License { get; set; }
    public string? Homepage { get; set; }
    public ComposerSource? Source { get; set; }
}

internal class PackagistDownloads
{
    public int Total { get; set; }
}
