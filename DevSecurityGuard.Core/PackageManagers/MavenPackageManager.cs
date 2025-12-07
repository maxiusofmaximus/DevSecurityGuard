using System.Text.Json;
using System.Xml.Linq;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// Maven (Java) package manager implementation
/// </summary>
public class MavenPackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "maven";
    public string DisplayName => "Maven (Java)";
    
    public string[] DetectionPatterns => new[]
    {
        "pom.xml"
    };
    
    public string[] LockFilePatterns => Array.Empty<string>(); // Maven doesn't use lock files
    
    public MavenPackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return File.Exists(Path.Combine(projectPath, "pom.xml"));
    }
    
    public string GetRegistryUrl()
    {
        return "https://repo1.maven.org/maven2";
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        try
        {
            var doc = await XDocument.LoadAsync(File.OpenRead(manifestPath), 
                LoadOptions.None, CancellationToken.None);
            
            XNamespace ns = "http://maven.apache.org/POM/4.0.0";
            
            // Parse dependencies
            var dependencies = doc.Descendants(ns + "dependency");
            foreach (var dep in dependencies)
            {
                var groupId = dep.Element(ns + "groupId")?.Value;
                var artifactId = dep.Element(ns + "artifactId")?.Value;
                var version = dep.Element(ns + "version")?.Value;
                var scope = dep.Element(ns + "scope")?.Value;
                
                if (!string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(artifactId))
                {
                    var name = $"{groupId}:{artifactId}";
                    
                    if (scope == "test")
                    {
                        manifest.DevDependencies[name] = version ?? "*";
                    }
                    else
                    {
                        manifest.Dependencies[name] = version ?? "*";
                    }
                }
            }
            
            // Parse project info
            manifest.Name = doc.Descendants(ns + "artifactId").FirstOrDefault()?.Value ?? "";
            manifest.Version = doc.Descendants(ns + "version").FirstOrDefault()?.Value ?? "";
            manifest.Description = doc.Descendants(ns + "description").FirstOrDefault()?.Value;
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return manifest;
    }
    
    public async Task<IEnumerable<PackageDependency>> ParseLockFileAsync(string lockFilePath)
    {
        // Maven doesn't use lock files
        return Enumerable.Empty<PackageDependency>();
    }
    
    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        try
        {
            // packageName format: "groupId:artifactId"
            var parts = packageName.Split(':');
            if (parts.Length != 2)
                return new PackageMetadata { Name = packageName };
            
            var groupId = parts[0];
            var artifactId = parts[1];
            
            // Use Maven Central REST API
            var url = $"https://search.maven.org/solrsearch/select?q=g:{groupId}+AND+a:{artifactId}&rows=1&wt=json";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<MavenSearchResult>(json);
            
            if (searchResult?.Response?.Docs == null || !searchResult.Response.Docs.Any())
                return new PackageMetadata { Name = packageName };
            
            var doc = searchResult.Response.Docs.First();
            
            return new PackageMetadata
            {
                Name = packageName,
                LatestVersion = doc.LatestVersion ?? "unknown",
                Repository = $"https://mvnrepository.com/artifact/{groupId}/{artifactId}"
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Top popular Maven artifacts
        return new[]
        {
            "org.springframework.boot:spring-boot-starter",
            "org.springframework:spring-core",
            "org.springframework:spring-context",
            "junit:junit",
            "org.junit.jupiter:junit-jupiter",
            "org.slf4j:slf4j-api",
            "ch.qos.logback:logback-classic",
            "com.google.guava:guava",
            "org.apache.commons:commons-lang3",
            "org.hibernate:hibernate-core",
            "com.fasterxml.jackson.core:jackson-databind",
            "org.projectlombok:lombok",
            "org.mockito:mockito-core",
            "javax.servlet:javax.servlet-api",
            "org.postgresql:postgresql"
        };
    }
}

// Maven Central API models
internal class MavenSearchResult
{
    public MavenResponse? Response { get; set; }
}

internal class MavenResponse
{
    public List<MavenDoc>? Docs { get; set; }
}

internal class MavenDoc
{
    [System.Text.Json.Serialization.JsonPropertyName("latestVersion")]
    public string? LatestVersion { get; set; }
}
