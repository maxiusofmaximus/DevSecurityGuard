using DevSecurityGuard.Service.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevSecurityGuard.Service.DetectionEngines;

/// <summary>
/// Detects supply chain attacks by validating package integrity and metadata
/// </summary>
public class SupplyChainDetector : IThreatDetector
{
    private readonly ILogger<SupplyChainDetector> _logger;
    private readonly HttpClient _httpClient;

    public string DetectorName => "Supply Chain Attack Detector";
    public int Priority => 80;

    public SupplyChainDetector(ILogger<SupplyChainDetector> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DevSecurityGuard/1.0");
    }

    public async Task<ThreatDetectionResult> AnalyzePackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing supply chain integrity for {PackageName}", packageName);

        try
        {
            // Fetch package metadata from npm registry
            var metadata = await FetchPackageMetadataAsync(packageName, cancellationToken);

            if (metadata == null)
            {
                _logger.LogWarning("Could not fetch metadata for {PackageName}", packageName);
                return ThreatDetectionResult.NoThreat(packageName, version);
            }

            // Check for recently published packages (potential supply chain attack vector)
            if (IsRecentlyPublished(metadata, out var publishDate))
            {
                return ThreatDetectionResult.CreateThreat(
                    ThreatType.SupplyChainAttack,
                    ThreatSeverity.Medium,
                    packageName,
                    $"Package was published very recently ({publishDate:yyyy-MM-dd HH:mm}). Consider waiting 24-48 hours before installing new packages.",
                    version);
            }

            // Check for maintainer changes in recent versions
            if (HasSuspiciousMaintainerChange(metadata))
            {
                return ThreatDetectionResult.CreateThreat(
                    ThreatType.SupplyChainAttack,
                    ThreatSeverity.High,
                    packageName,
                    "Package maintainer has changed recently. This could indicate a compromised package.",
                    version);
            }

            // Check for unusual version jumps
            if (HasUnusualVersionJump(metadata, out var versionInfo))
            {
                return ThreatDetectionResult.CreateThreat(
                    ThreatType.SupplyChainAttack,
                    ThreatSeverity.Medium,
                    packageName,
                    $"Unusual version jump detected: {versionInfo}. This is uncommon and may indicate compromise.",
                    version);
            }

            return ThreatDetectionResult.NoThreat(packageName, version);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching package metadata for {PackageName}", packageName);
            return ThreatDetectionResult.NoThreat(packageName, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error analyzing {PackageName}", packageName);
            return ThreatDetectionResult.NoThreat(packageName, version);
        }
    }

    private async Task<NpmPackageMetadata?> FetchPackageMetadataAsync(string packageName, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://registry.npmjs.org/{Uri.EscapeDataString(packageName)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("npm registry returned {StatusCode} for {PackageName}", response.StatusCode, packageName);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<NpmPackageMetadata>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing npm metadata for {PackageName}", packageName);
            return null;
        }
    }

    private bool IsRecentlyPublished(NpmPackageMetadata metadata, out DateTime publishDate)
    {
        publishDate = DateTime.UtcNow;

        if (metadata.Time?.Created == null)
            return false;

        publishDate = metadata.Time.Created;
        var hoursSincePublish = (DateTime.UtcNow - publishDate).TotalHours;

        // Flag packages published in the last 24 hours
        return hoursSincePublish < 24;
    }

    private bool HasSuspiciousMaintainerChange(NpmPackageMetadata metadata)
    {
        // Check if maintainer changed in recent versions
        // This is a simplified check - in production, would compare historical maintainers
        if (metadata.Versions == null || metadata.Versions.Count < 2)
            return false;

        // Get the two most recent versions
        var versions = metadata.Versions
            .OrderByDescending(v => v.Value.PublishTime ?? DateTime.MinValue)
            .Take(2)
            .ToList();

        if (versions.Count < 2)
            return false;

        var latestMaintainers = versions[0].Value.Maintainers?.Select(m => m.Name).ToHashSet() ?? new HashSet<string>();
        var previousMaintainers = versions[1].Value.Maintainers?.Select(m => m.Name).ToHashSet() ?? new HashSet<string>();

        // Check if there are new maintainers
        var newMaintainers = latestMaintainers.Except(previousMaintainers).ToList();

        return newMaintainers.Count > 0;
    }

    private bool HasUnusualVersionJump(NpmPackageMetadata metadata, out string versionInfo)
    {
        versionInfo = string.Empty;

        if (metadata.Versions == null || metadata.Versions.Count < 2)
            return false;

        var sortedVersions = metadata.Versions
            .OrderBy(v => v.Value.PublishTime ?? DateTime.MinValue)
            .Select(v => v.Key)
            .ToList();

        if (sortedVersions.Count < 2)
            return false;

        var latest = sortedVersions.Last();
        var previous = sortedVersions[sortedVersions.Count - 2];

        // Parse semantic versions
        if (TryParseVersion(previous, out var prevMajor, out var prevMinor, out var prevPatch) &&
            TryParseVersion(latest, out var latestMajor, out var latestMinor, out var latestPatch))
        {
            // Check for major version jump > 5
            if (latestMajor - prevMajor > 5)
            {
                versionInfo = $"{previous} → {latest} (major version jump)";
                return true;
            }

            // Check for minor version jump > 50 within same major
            if (latestMajor == prevMajor && latestMinor - prevMinor > 50)
            {
                versionInfo = $"{previous} → {latest} (large minor version jump)";
                return true;
            }
        }

        return false;
    }

    private bool TryParseVersion(string version, out int major, out int minor, out int patch)
    {
        major = minor = patch = 0;

        // Remove 'v' prefix if present
        version = version.TrimStart('v');

        var parts = version.Split('.');
        if (parts.Length < 3)
            return false;

        return int.TryParse(parts[0], out major) &&
               int.TryParse(parts[1], out minor) &&
               int.TryParse(parts[2].Split('-')[0], out patch); // Handle pre-release versions
    }
}

// Data models for npm registry API
public class NpmPackageMetadata
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("versions")]
    public Dictionary<string, NpmVersionInfo>? Versions { get; set; }

    [JsonPropertyName("time")]
    public NpmTimeInfo? Time { get; set; }
}

public class NpmVersionInfo
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("maintainers")]
    public List<NpmMaintainer>? Maintainers { get; set; }

    [JsonIgnore]
    public DateTime? PublishTime { get; set; }
}

public class NpmMaintainer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class NpmTimeInfo
{
    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; }
}
