using System.Text.Json;
using System.Xml.Linq;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// NuGet (.NET) package manager implementation
/// </summary>
public class NuGetPackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "nuget";
    public string DisplayName => "NuGet (.NET)";
    
    public string[] DetectionPatterns => new[]
    {
        "*.csproj",
        "*.vbproj",
        "*.fsproj",
        "packages.config",
        "project.json"
    };
    
    public string[] LockFilePatterns => new[]
    {
        "packages.lock.json",
        "project.assets.json"
    };
    
    public NuGetPackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return Directory.GetFiles(projectPath, "*.csproj").Any() ||
               Directory.GetFiles(projectPath, "*.vbproj").Any() ||
               Directory.GetFiles(projectPath, "*.fsproj").Any() ||
               File.Exists(Path.Combine(projectPath, "packages.config"));
    }
    
    public string GetRegistryUrl()
    {
        return "https://api.nuget.org/v3";
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        if (manifestPath.EndsWith(".csproj") || manifestPath.EndsWith(".vbproj") || manifestPath.EndsWith(".fsproj"))
        {
            return await ParseProjectFileAsync(manifestPath);
        }
        else if (manifestPath.EndsWith("packages.config"))
        {
            return await ParsePackagesConfigAsync(manifestPath);
        }
        
        return manifest;
    }
    
    private async Task<PackageManifest> ParseProjectFileAsync(string filePath)
    {
        var manifest = new PackageManifest();
        
        try
        {
            var doc = await XDocument.LoadAsync(File.OpenRead(filePath), 
                LoadOptions.None, CancellationToken.None);
            
            // Parse PackageReference elements (SDK-style projects)
            var packageRefs = doc.Descendants("PackageReference");
            foreach (var pkg in packageRefs)
            {
                var name = pkg.Attribute("Include")?.Value;
                var version = pkg.Attribute("Version")?.Value;
                
                if (!string.IsNullOrEmpty(name))
                {
                    manifest.Dependencies[name] = version ?? "*";
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return manifest;
    }
    
    private async Task<PackageManifest> ParsePackagesConfigAsync(string filePath)
    {
        var manifest = new PackageManifest();
        
        try
        {
            var doc = await XDocument.LoadAsync(File.OpenRead(filePath), 
                LoadOptions.None, CancellationToken.None);
            
            var packages = doc.Descendants("package");
            foreach (var pkg in packages)
            {
                var name = pkg.Attribute("id")?.Value;
                var version = pkg.Attribute("version")?.Value;
                
                if (!string.IsNullOrEmpty(name))
                {
                    manifest.Dependencies[name] = version ?? "*";
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
        
        if (lockFilePath.EndsWith("packages.lock.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(lockFilePath);
                var lockData = JsonSerializer.Deserialize<NuGetLockFile>(json);
                
                if (lockData?.Dependencies != null)
                {
                    foreach (var (framework, packages) in lockData.Dependencies)
                    {
                        foreach (var (name, info) in packages)
                        {
                            dependencies.Add(new PackageDependency
                            {
                                Name = name,
                                Version = info.Resolved ?? "*",
                                ResolvedVersion = info.Resolved,
                                IsDev = false,
                                Source = "nuget"
                            });
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }
        
        return dependencies;
    }
    
    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        try
        {
            // Use NuGet API v3
            var url = $"{GetRegistryUrl()}/registration5-semver1/{packageName.ToLowerInvariant()}/index.json";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var packageData = JsonSerializer.Deserialize<NuGetPackageData>(json);
            
            if (packageData?.Items == null || !packageData.Items.Any())
                return new PackageMetadata { Name = packageName };
            
            var latestItem = packageData.Items.Last().Items?.Last();
            
            return new PackageMetadata
            {
                Name = packageName,
                LatestVersion = latestItem?.CatalogEntry?.Version ?? "unknown",
                Description = latestItem?.CatalogEntry?.Description,
                Author = latestItem?.CatalogEntry?.Authors,
                License = latestItem?.CatalogEntry?.LicenseUrl
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Top popular NuGet packages
        return new[]
        {
            "Newtonsoft.Json", "Microsoft.Extensions.DependencyInjection", 
            "Microsoft.Extensions.Logging", "System.Text.Json",
            "EntityFrameworkCore", "Serilog", "AutoMapper", "Dapper",
            "Moq", "xunit", "NUnit", "FluentAssertions",
            "Polly", "MediatR", "Swashbuckle.AspNetCore", "IdentityServer4",
            "Microsoft.AspNetCore.Mvc", "Microsoft.EntityFrameworkCore.SqlServer",
            "Microsoft.Extensions.Configuration", "Microsoft.Extensions.Hosting"
        };
    }
}

// NuGet lock file models
internal class NuGetLockFile
{
    public Dictionary<string, Dictionary<string, NuGetLockPackage>>? Dependencies { get; set; }
}

internal class NuGetLockPackage
{
    public string? Resolved { get; set; }
}

// NuGet API models
internal class NuGetPackageData
{
    public List<NuGetPackageItem>? Items { get; set; }
}

internal class NuGetPackageItem
{
    public List<NuGetPackageVersion>? Items { get; set; }
}

internal class NuGetPackageVersion
{
    public NuGetCatalogEntry? CatalogEntry { get; set; }
}

internal class NuGetCatalogEntry
{
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Authors { get; set; }
    public string? LicenseUrl { get; set; }
}
