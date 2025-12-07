using DevSecurityGuard.Service.Models;

namespace DevSecurityGuard.Service.DetectionEngines;

/// <summary>
/// Base interface for all threat detection engines
/// </summary>
public interface IThreatDetector
{
    /// <summary>
    /// Name of the detector
    /// </summary>
    string DetectorName { get; }

    /// <summary>
    /// Analyze a package for threats
    /// </summary>
    Task<ThreatDetectionResult> AnalyzePackageAsync(string packageName, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Priority of this detector (higher runs first)
    /// </summary>
    int Priority { get; }
}
