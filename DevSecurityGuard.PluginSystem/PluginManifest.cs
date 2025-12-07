using System.Text.Json;

namespace DevSecurityGuard.PluginSystem;

/// <summary>
/// Plugin manifest metadata
/// </summary>
public class PluginManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PluginType Type { get; set; }
    public string AssemblyPath { get; set; } = string.Empty;
    public string EntryPoint { get; set; } = string.Empty;
    public string[]? SupportedPackageManagers { get; set; }
    public int Priority { get; set; } = 50;
    public Dictionary<string, string>? Dependencies { get; set; }
    public string[]? Tags { get; set; }
    public string? Homepage { get; set; }
    public string? Repository { get; set; }
    public string? License { get; set; }
    
    /// <summary>
    /// Load manifest from JSON file
    /// </summary>
    public static async Task<PluginManifest?> LoadFromFileAsync(string manifestPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            return JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Save manifest to JSON file
    /// </summary>
    public async Task SaveToFileAsync(string manifestPath)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await File.WriteAllTextAsync(manifestPath, json);
    }
    
    /// <summary>
    /// Validate manifest
    /// </summary>
    public (bool IsValid, string[] Errors) Validate()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Plugin ID is required");
        
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Plugin name is required");
        
        if (string.IsNullOrWhiteSpace(Version))
            errors.Add("Plugin version is required");
        
        if (string.IsNullOrWhiteSpace(AssemblyPath))
            errors.Add("Assembly path is required");
        
        if (string.IsNullOrWhiteSpace(EntryPoint))
            errors.Add("Entry point is required");
        
        // Validate version format (semantic versioning)
        if (!string.IsNullOrWhiteSpace(Version))
        {
            var parts = Version.Split('.');
            if (parts.Length < 2 || parts.Length > 3)
                errors.Add("Version must be in format 'major.minor' or 'major.minor.patch'");
        }
        
        return (errors.Count == 0, errors.ToArray());
    }
}

/// <summary>
/// Plugin types
/// </summary>
public enum PluginType
{
    Detector,
    PackageManager,
    Output,
    Other
}
