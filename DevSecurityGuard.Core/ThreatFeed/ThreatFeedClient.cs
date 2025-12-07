using System.Text.Json;

namespace DevSecurityGuard.Core.ThreatFeed;

/// <summary>
/// Phase 8: Threat Intelligence Feed (opt-in)
/// </summary>
public class ThreatFeedClient
{
    private readonly HttpClient _httpClient;
    private readonly string _feedUrl;
    private readonly string _cacheDir;

    public ThreatFeedClient(string feedUrl = "https://feed.devsecurityguard.io/v1")
    {
        _httpClient = new HttpClient();
        _feedUrl = feedUrl;
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevSecurityGuard",
            "feed");
        
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<ThreatFeed?> FetchLatestAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_feedUrl}/threats/latest");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var feed = JsonSerializer.Deserialize<ThreatFeed>(json);

            // Cache locally
            if (feed != null)
            {
                var cachePath = Path.Combine(_cacheDir, "threats.json");
                await File.WriteAllTextAsync(cachePath, json);
            }

            return feed;
        }
        catch
        {
            // Fallback to cached version
            return LoadCached();
        }
    }

    public ThreatFeed? LoadCached()
    {
        var cachePath = Path.Combine(_cacheDir, "threats.json");
        
        if (!File.Exists(cachePath))
            return null;

        try
        {
            var json = File.ReadAllText(cachePath);
            return JsonSerializer.Deserialize<ThreatFeed>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsPackageMalicious(string packageName, string packageManager)
    {
        var feed = await FetchLatestAsync() ?? LoadCached();
        
        if (feed == null)
            return false;

        return feed.Threats.Any(t => 
            t.Package.Equals(packageName, StringComparison.OrdinalIgnoreCase) &&
            t.PackageManager.Equals(packageManager, StringComparison.OrdinalIgnoreCase));
    }
}

public class ThreatFeed
{
    public string Version { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public List<ThreatEntry> Threats { get; set; } = new();
    public Dictionary<string, ModelInfo> Models { get; set; } = new();
}

public class ThreatEntry
{
    public string Package { get; set; } = "";
    public string PackageManager { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Signatures { get; set; } = Array.Empty<string>();
    public DateTime AddedAt { get; set; }
}

public class ModelInfo
{
    public string Version { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
}
