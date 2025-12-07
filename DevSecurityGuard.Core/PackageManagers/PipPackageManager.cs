using System.Text.Json;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// Python pip package manager implementation
/// </summary>
public class PipPackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "pip";
    public string DisplayName => "pip (Python)";
    
    public string[] DetectionPatterns => new[]
    {
        "requirements.txt",
        "Pipfile",
        "pyproject.toml",
        "setup.py"
    };
    
    public string[] LockFilePatterns => new[]
    {
        "Pipfile.lock",
        "poetry.lock",
        "requirements-lock.txt"
    };
    
    public PipPackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return DetectionPatterns.Any(pattern => 
            File.Exists(Path.Combine(projectPath, pattern)));
    }
    
    public string GetRegistryUrl()
    {
        return "https://pypi.org/pypi";
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        if (manifestPath.EndsWith("requirements.txt"))
        {
            return await ParseRequirementsTxtAsync(manifestPath);
        }
        else if (manifestPath.EndsWith("Pipfile"))
        {
            return await ParsePipfileAsync(manifestPath);
        }
        else if (manifestPath.EndsWith("pyproject.toml"))
        {
            return await ParsePyprojectTomlAsync(manifestPath);
        }
        
        return manifest;
    }
    
    private async Task<PackageManifest> ParseRequirementsTxtAsync(string filePath)
    {
        var manifest = new PackageManifest();
        var lines = await File.ReadAllLinesAsync(filePath);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip comments and empty lines
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;
            
            // Parse package==version or package>=version format
            var parts = trimmed.Split(new[] { "==", ">=", "<=", "~=", "!=" }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 1)
            {
                var packageName = parts[0].Trim();
                var version = parts.Length > 1 ? parts[1].Trim() : "*";
                
                manifest.Dependencies[packageName] = version;
            }
        }
        
        return manifest;
    }
    
    private async Task<PackageManifest> ParsePipfileAsync(string filePath)
    {
        // TODO: Implement TOML parsing for Pipfile
        // For now, return empty manifest
        return new PackageManifest();
    }
    
    private async Task<PackageManifest> ParsePyprojectTomlAsync(string filePath)
    {
        // TODO: Implement TOML parsing for pyproject.toml
        // For now, return empty manifest
        return new PackageManifest();
    }
    
    public async Task<IEnumerable<PackageDependency>> ParseLockFileAsync(string lockFilePath)
    {
        var dependencies = new List<PackageDependency>();
        
        if (lockFilePath.EndsWith("Pipfile.lock"))
        {
            // Parse Pipfile.lock (JSON format)
            var json = await File.ReadAllTextAsync(lockFilePath);
            var lockData = JsonSerializer.Deserialize<PipfileLock>(json);
            
            if (lockData?.Default != null)
            {
                foreach (var (name, info) in lockData.Default)
                {
                    dependencies.Add(new PackageDependency
                    {
                        Name = name,
                        Version = info.Version?.TrimStart('=') ?? "*",
                        ResolvedVersion = info.Version?.TrimStart('='),
                        IsDev = false,
                        Source = "pypi"
                    });
                }
            }
            
            if (lockData?.Develop != null)
            {
                foreach (var (name, info) in lockData.Develop)
                {
                    dependencies.Add(new PackageDependency
                    {
                        Name = name,
                        Version = info.Version?.TrimStart('=') ?? "*",
                        ResolvedVersion = info.Version?.TrimStart('='),
                        IsDev = true,
                        Source = "pypi"
                    });
                }
            }
        }
        
        return dependencies;
    }
    
    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageName, string? version = null)
    {
        try
        {
            var url = version != null
                ? $"{GetRegistryUrl()}/{packageName}/{version}/json"
                : $"{GetRegistryUrl()}/{packageName}/json";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var pypiData = JsonSerializer.Deserialize<PyPIPackageData>(json);
            
            if (pypiData?.Info == null)
                return new PackageMetadata { Name = packageName };
            
            return new PackageMetadata
            {
                Name = pypiData.Info.Name ?? packageName,
                LatestVersion = pypiData.Info.Version ?? "unknown",
                Description = pypiData.Info.Summary,
                Author = pypiData.Info.Author,
                License = pypiData.Info.License,
                Homepage = pypiData.Info.HomePage,
                Repository = pypiData.Info.ProjectUrl
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Top 100 most popular Python packages
        return new[]
        {
            "requests", "urllib3", "certifi", "charset-normalizer", "idna",
            "numpy", "pandas", "matplotlib", "scipy", "scikit-learn",
            "tensorflow", "torch", "keras", "pytest", "setuptools",
            "wheel", "pip", "black", "flake8", "mypy",
            "django", "flask", "fastapi", "sqlalchemy", "celery",
            "redis", "boto3", "awscli", "pyyaml", "jinja2",
            "click", "python-dateutil", "pytz", "six", "packaging"
        };
    }
}

// PyPI API response models
internal class PipfileLock
{
    public Dictionary<string, PipfileLockPackage>? Default { get; set; }
    public Dictionary<string, PipfileLockPackage>? Develop { get; set; }
}

internal class PipfileLockPackage
{
    public string? Version { get; set; }
}

internal class PyPIPackageData
{
    public PyPIInfo? Info { get; set; }
}

internal class PyPIInfo
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Summary { get; set; }
    public string? Author { get; set; }
    public string? License { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("home_page")]
    public string? HomePage { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("project_url")]
    public string? ProjectUrl { get; set; }
}
