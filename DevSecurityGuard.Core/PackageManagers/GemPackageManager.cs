using System.Text.Json;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// RubyGems package manager implementation
/// </summary>
public class GemPackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "gem";
    public string DisplayName => "RubyGems (Ruby)";
    
    public string[] DetectionPatterns => new[]
    {
        "Gemfile",
        "*.gemspec"
    };
    
    public string[] LockFilePatterns => new[]
    {
        "Gemfile.lock"
    };
    
    public GemPackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return File.Exists(Path.Combine(projectPath, "Gemfile")) ||
               Directory.GetFiles(projectPath, "*.gemspec").Any();
    }
    
    public string GetRegistryUrl()
    {
        return "https://rubygems.org/api/v1";
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        if (manifestPath.EndsWith("Gemfile"))
        {
            var content = await File.ReadAllTextAsync(manifestPath);
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Match: gem 'name', 'version'
                // Match: gem "name", "version"
                if (trimmed.StartsWith("gem "))
                {
                    var parts = trimmed.Split(new[] { '\'', '"', ',' }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 2)
                    {
                        var name = parts[1].Trim();
                        var version = parts.Length > 2 ? parts[2].Trim() : "*";
                        
                        // Check if it's in a group (like :development)
                        var isDev = trimmed.Contains(":development") || 
                                   trimmed.Contains("group: :development");
                        
                        if (isDev)
                        {
                            manifest.DevDependencies[name] = version;
                        }
                        else
                        {
                            manifest.Dependencies[name] = version;
                        }
                    }
                }
            }
        }
        
        return manifest;
    }
    
    public async Task<IEnumerable<PackageDependency>> ParseLockFileAsync(string lockFilePath)
    {
        var dependencies = new List<PackageDependency>();
        
        if (File.Exists(lockFilePath))
        {
            var content = await File.ReadAllTextAsync(lockFilePath);
            var lines = content.Split('\n');
            
            bool inSpecs = false;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed == "specs:")
                {
                    inSpecs = true;
                    continue;
                }
                
                if (inSpecs && trimmed.StartsWith("PLATFORMS") || trimmed.StartsWith("DEPENDENCIES"))
                {
                    inSpecs = false;
                    continue;
                }
                
                if (inSpecs && trimmed.Contains('(') && trimmed.Contains(')'))
                {
                    // Format: name (version)
                    var parts = trimmed.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var name = parts[0].Trim();
                        var version = parts[1].Trim();
                        
                        dependencies.Add(new PackageDependency
                        {
                            Name = name,
                            Version = version,
                            ResolvedVersion = version,
                            IsDev = false,
                            Source = "rubygems"
                        });
                    }
                }
            }
        }
        
        return dependencies;
    }
    
    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        try
        {
            var url = $"{GetRegistryUrl()}/gems/{packageName}.json";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var gemData = JsonSerializer.Deserialize<RubyGemsData>(json);
            
            if (gemData == null)
                return new PackageMetadata { Name = packageName };
            
            return new PackageMetadata
            {
                Name = gemData.Name ?? packageName,
                LatestVersion = gemData.Version ?? "unknown",
                Description = gemData.Info,
                Author = gemData.Authors,
                License = gemData.Licenses?.FirstOrDefault(),
                Homepage = gemData.HomepageUri,
                Repository = gemData.SourceCodeUri,
                DownloadCount = gemData.Downloads
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Popular Ruby gems
        return new[]
        {
            "rails", "rake", "bundler", "rspec", "puma",
            "devise", "sidekiq", "pg", "mysql2", "redis",
            "nokogiri", "capybara", "factory_bot", "rubocop", "pundit",
            "kaminari", "carrierwave", "paperclip", "activerecord", "actionpack"
        };
    }
}

// RubyGems API models
internal class RubyGemsData
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Info { get; set; }
    public string? Authors { get; set; }
    public List<string>? Licenses { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("homepage_uri")]
    public string? HomepageUri { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("source_code_uri")]
    public string? SourceCodeUri { get; set; }
    
    public int Downloads { get; set; }
}
