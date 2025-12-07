using System.Text.Json;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// Gradle (Java/Kotlin/Android) package manager implementation
/// </summary>
public class GradlePackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "gradle";
    public string DisplayName => "Gradle (Java/Kotlin)";
    
    public string[] DetectionPatterns => new[]
    {
        "build.gradle",
        "build.gradle.kts",
        "settings.gradle",
        "settings.gradle.kts"
    };
    
    public string[] LockFilePatterns => new[]
    {
        "gradle.lockfile",
        "buildscript-gradle.lockfile"
    };
    
    public GradlePackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return File.Exists(Path.Combine(projectPath, "build.gradle")) ||
               File.Exists(Path.Combine(projectPath, "build.gradle.kts"));
    }
    
    public string GetRegistryUrl()
    {
        return "https://repo1.maven.org/maven2"; // Uses Maven Central by default
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        // Gradle build files are Groovy/Kotlin scripts, complex to parse
        // For now, simple regex-based approach
        var content = await File.ReadAllTextAsync(manifestPath);
        var lines = content.Split('\n');
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Match: implementation 'group:artifact:version'
            // Match: testImplementation "group:artifact:version"
            if (trimmed.Contains("implementation") || trimmed.Contains("api") || 
                trimmed.Contains("compile"))
            {
                var parts = trimmed.Split(new[] { '\'', '"' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var dependency = parts[1];
                    if (dependency.Contains(':'))
                    {
                        var depParts = dependency.Split(':');
                        if (depParts.Length >= 2)
                        {
                            var name = $"{depParts[0]}:{depParts[1]}";
                            var version = depParts.Length > 2 ? depParts[2] : "*";
                            
                            if (trimmed.Contains("test"))
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
        }
        
        return manifest;
    }
    
    public async Task<IEnumerable<PackageDependency>> ParseLockFileAsync(string lockFilePath)
    {
        var dependencies = new List<PackageDependency>();
        
        // Gradle lockfile format is simple text
        if (File.Exists(lockFilePath))
        {
            var lines = await File.ReadAllLinesAsync(lockFilePath);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                    continue;
                
                // Format: group:artifact:version=classifier
                var parts = trimmed.Split('=')[0].Split(':');
                if (parts.Length >= 3)
                {
                    dependencies.Add(new PackageDependency
                    {
                        Name = $"{parts[0]}:{parts[1]}",
                        Version = parts[2],
                        ResolvedVersion = parts[2],
                        IsDev = false,
                        Source = "maven"
                    });
                }
            }
        }
        
        return dependencies;
    }
    
    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        // Gradle uses Maven repos, delegate to Maven search
        try
        {
            var parts = packageName.Split(':');
            if (parts.Length != 2)
                return new PackageMetadata { Name = packageName };
            
            var groupId = parts[0];
            var artifactId = parts[1];
            
            var url = $"https://search.maven.org/solrsearch/select?q=g:{groupId}+AND+a:{artifactId}&rows=1&wt=json";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<GradleSearchResult>(json);
            
            if (searchResult?.Response?.Docs == null || !searchResult.Response.Docs.Any())
                return new PackageMetadata { Name = packageName };
            
            var doc = searchResult.Response.Docs.First();
            
            return new PackageMetadata
            {
                Name = packageName,
                LatestVersion = doc.LatestVersion ?? "unknown"
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Popular Gradle/Android dependencies
        return new[]
        {
            "androidx.appcompat:appcompat",
            "androidx.core:core-ktx",
            "com.google.android.material:material",
            "androidx.constraintlayout:constraintlayout",
            "org.jetbrains.kotlin:kotlin-stdlib",
            "com.squareup.retrofit2:retrofit",
            "com.squareup.okhttp3:okhttp",
            "io.reactivex.rxjava3:rxjava",
            "com.google.dagger:dagger",
            "androidx.lifecycle:lifecycle-viewmodel-ktx"
        };
    }
}

// Gradle search models (reuse Maven models)
internal class GradleSearchResult
{
    public GradleResponse? Response { get; set; }
}

internal class GradleResponse
{
    public List<GradleDoc>? Docs { get; set; }
}

internal class GradleDoc
{
    [System.Text.Json.Serialization.JsonPropertyName("latestVersion")]
    public string? LatestVersion { get; set; }
}
