using System.Text.Json;
using DevSecurityGuard.Core.Abstractions;

namespace DevSecurityGuard.Core.PackageManagers;

/// <summary>
/// npm (JavaScript/Node.js) package manager implementation
/// </summary>
public class NpmPackageManager : IPackageManager
{
    private readonly HttpClient _httpClient;
    
    public string Name => "npm";
    public string DisplayName => "npm (Node.js)";
    
    public string[] DetectionPatterns => new[]
    {
        "package.json"
    };
    
    public string[] LockFilePatterns => new[]
    {
        "package-lock.json",
        "npm-shrinkwrap.json",
        "yarn.lock",
        "pnpm-lock.yaml"
    };
    
    public NpmPackageManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }
    
    public bool IsDetected(string projectPath)
    {
        return File.Exists(Path.Combine(projectPath, "package.json"));
    }
    
    public string GetRegistryUrl()
    {
        return "https://registry.npmjs.org";
    }
    
    public async Task<PackageManifest> ParseManifestAsync(string manifestPath)
    {
        var manifest = new PackageManifest();
        
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            var packageJson = JsonSerializer.Deserialize<NpmPackageJson>(json);
            
            if (packageJson == null)
                return manifest;
            
            manifest.Name = packageJson.Name ?? "";
            manifest.Version = packageJson.Version ?? "";
            manifest.Description = packageJson.Description;
            manifest.Author = packageJson.Author;
            manifest.License = packageJson.License;
            manifest.Scripts = packageJson.Scripts;
            
            // Parse dependencies
            if (packageJson.Dependencies != null)
            {
                foreach (var (name, version) in packageJson.Dependencies)
                {
                    manifest.Dependencies[name] = version;
                }
            }
            
            // Parse dev dependencies
            if (packageJson.DevDependencies != null)
            {
                foreach (var (name, version) in packageJson.DevDependencies)
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
        
        if (lockFilePath.EndsWith("package-lock.json") || lockFilePath.EndsWith("npm-shrinkwrap.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(lockFilePath);
                var lockData = JsonSerializer.Deserialize<NpmLockFile>(json);
                
                if (lockData?.Dependencies != null)
                {
                    foreach (var (name, info) in lockData.Dependencies)
                    {
                        dependencies.Add(new PackageDependency
                        {
                            Name = name,
                            Version = info.Version ?? "*",
                            ResolvedVersion = info.Resolved,
                            IsDev = info.Dev,
                            Source = "npm"
                        });
                    }
                }
                
                // npm v2+: packages field
                if (lockData?.Packages != null)
                {
                    foreach (var (path, info) in lockData.Packages)
                    {
                        if (string.IsNullOrEmpty(path) || path == "")
                            continue;
                        
                        var name = path.StartsWith("node_modules/") 
                            ? path.Substring("node_modules/".Length)
                            : path;
                        
                        dependencies.Add(new PackageDependency
                        {
                            Name = name,
                            Version = info.Version ?? "*",
                            ResolvedVersion = info.Resolved,
                            IsDev = info.Dev,
                            Source = "npm"
                        });
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
            var url = version != null
                ? $"{GetRegistryUrl()}/{packageName}/{version}"
                : $"{GetRegistryUrl()}/{packageName}/latest";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var packageData = JsonSerializer.Deserialize<NpmPackageData>(json);
            
            if (packageData == null)
                return new PackageMetadata { Name = packageName };
            
            return new PackageMetadata
            {
                Name = packageData.Name ?? packageName,
                LatestVersion = packageData.Version ?? "unknown",
                Description = packageData.Description,
                Author = packageData.Author,
                License = packageData.License,
                Homepage = packageData.Homepage,
                Repository = packageData.Repository?.Url,
                PublishedAt = packageData.Time?.Modified
            };
        }
        catch
        {
            return new PackageMetadata { Name = packageName };
        }
    }
    
    public async Task<IEnumerable<string>> GetPopularPackagesAsync()
    {
        // Top 100 most popular npm packages
        return new[]
        {
            "react", "lodash", "chalk", "axios", "express",
            "commander", "typescript", "webpack", "eslint", "prettier",
            "moment", "request", "vue", "angular", "next",
            "jquery", "babel-core", "tslib", "debug", "minimist",
            "react-dom", "prop-types", "classnames", "uuid", "dotenv",
            "cors", "body-parser", "nodemon", "jest", "mocha",
            "webpack-cli", "ts-node", "dayjs", "rxjs", "socket.io",
            "bcrypt", "jsonwebtoken", "passport", "mongoose", "sequelize",
            "redux", "react-router-dom", "styled-components", "material-ui", "antd"
        };
    }
}

// npm package.json models
internal class NpmPackageJson
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? License { get; set; }
    public Dictionary<string, string>? Scripts { get; set; }
    public Dictionary<string, string>? Dependencies { get; set; }
    public Dictionary<string, string>? DevDependencies { get; set; }
}

// npm lock file models
internal class NpmLockFile
{
    public Dictionary<string, NpmLockPackage>? Dependencies { get; set; }
    public Dictionary<string, NpmLockPackage>? Packages { get; set; }
}

internal class NpmLockPackage
{
    public string? Version { get; set; }
    public string? Resolved { get; set; }
    public bool Dev { get; set; }
}

// npm registry models
internal class NpmPackageData
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? License { get; set; }
    public string? Homepage { get; set; }
    public NpmRepository? Repository { get; set; }
    public NpmTime? Time { get; set; }
}

internal class NpmRepository
{
    public string? Type { get; set; }
    public string? Url { get; set; }
}

internal class NpmTime
{
    public DateTime? Modified { get; set; }
}
