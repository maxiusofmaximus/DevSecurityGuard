using System.Net.Http.Json;

namespace DevSecurityGuard.Core.Detectors;

/// <summary>
/// Phase 9: Enhanced Detector - Dependency Confusion Attack
/// </summary>
public class DependencyConfusionDetector
{
    private readonly HashSet<string> _internalScopes;

    public DependencyConfusionDetector(IEnumerable<string> internalScopes)
    {
        _internalScopes = new HashSet<string>(internalScopes, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsConfusionAttack(string packageName, string registry)
    {
        // Check if package uses internal scope but comes from public registry
        foreach (var scope in _internalScopes)
        {
            if (packageName.StartsWith(scope, StringComparison.OrdinalIgnoreCase))
            {
                // Internal package should NOT come from public registry
                if (IsPublicRegistry(registry))
                {
                    return true; // Confusion attack detected
                }
            }
        }

        return false;
    }

    private bool IsPublicRegistry(string registry)
    {
        var publicRegistries = new[]
        {
            "registry.npmjs.org",
            "pypi.org",
            "crates.io",
            "nuget.org"
        };

        return publicRegistries.Any(r => registry.Contains(r, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Phase 9: Enhanced Detector - License Compliance
/// </summary>
public class LicenseComplianceDetector
{
    private readonly HashSet<string> _allowedLicenses;
    private readonly HashSet<string> _bannedLicenses;

    public LicenseComplianceDetector()
    {
        _allowedLicenses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MIT", "Apache-2.0", "BSD-3-Clause", "ISC", "BSD-2-Clause"
        };

        _bannedLicenses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GPL-3.0", "AGPL-3.0", "LGPL-3.0" // Copyleft licenses
        };
    }

    public (bool IsCompliant, string Reason) CheckLicense(string license)
    {
        if (string.IsNullOrWhiteSpace(license))
        {
            return (false, "No license specified");
        }

        if (_bannedLicenses.Contains(license))
        {
            return (false, $"Banned license: {license}");
        }

        if (!_allowedLicenses.Contains(license))
        {
            return (false, $"License not in allowed list: {license}");
        }

        return (true, "Compliant");
    }
}

/// <summary>
/// Phase 9: Enhanced Detector - Vulnerability Scanner (OSV integration)
/// </summary>
public class VulnerabilityDetector
{
    private readonly HttpClient _httpClient;

    public VulnerabilityDetector()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Vulnerability>> CheckVulnerabilitiesAsync(string packageName, string version, string ecosystem)
    {
        try
        {
            // OSV.dev API - simplified for now
            var requestBody = System.Text.Json.JsonSerializer.Serialize(new
            {
                package_name = packageName,
                version = version,
                ecosystem = MapEcosystem(ecosystem)
            });

            var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.osv.dev/v1/query", content);

            if (!response.IsSuccessStatusCode)
                return new List<Vulnerability>();

            var json = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<OsvResponse>(json);
            
            return result?.Vulns?.Select(v => new Vulnerability
            {
                Id = v.Id,
                Summary = v.Summary,
                Severity = v.Severity,
                FixedIn = v.FixedIn
            }).ToList() ?? new List<Vulnerability>();
        }
        catch
        {
            return new List<Vulnerability>();
        }
    }

    private string MapEcosystem(string pm)
    {
        return pm.ToLower() switch
        {
            "npm" => "npm",
            "pip" => "PyPI",
            "cargo" => "crates.io",
            "nuget" => "NuGet",
            "maven" => "Maven",
            "gem" => "RubyGems",
            "composer" => "Packagist",
            _ => pm
        };
    }
}

public class Vulnerability
{
    public string Id { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Severity { get; set; } = "";
    public string? FixedIn { get; set; }
}

class OsvResponse
{
    public List<OsvVuln>? Vulns { get; set; }
}

class OsvVuln
{
    public string Id { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Severity { get; set; } = "";
    public string? FixedIn { get; set; }
}
