using DevSecurityGuard.Service.DetectionEngines;
using DevSecurityGuard.Service.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DevSecurityGuard.Tests.UnitTests;

public class TyposquattingDetectorTests
{
    private readonly Mock<ILogger<TyposquattingDetector>> _loggerMock;
    private readonly TyposquattingDetector _detector;

    public TyposquattingDetectorTests()
    {
        _loggerMock = new Mock<ILogger<TyposquattingDetector>>();
        _detector = new TyposquattingDetector(_loggerMock.Object);
    }

    [Fact]
    public async Task AnalyzePackageAsync_PopularPackage_ReturnsNoThreat()
    {
        // Arrange
        var packageName = "react";

        // Act
        var result = await _detector.AnalyzePackageAsync(packageName);

        // Assert
        Assert.False(result.IsThreatDetected);
        Assert.Equal(packageName, result.PackageName);
    }

    [Theory]
    [InlineData("reqest", "request")] // 1 character difference
    [InlineData("reacr", "react")] // 1 character difference  
    [InlineData("expresss", "express")] // 1 extra character
    public async Task AnalyzePackageAsync_TyposquattedPackage_ReturnsThreat(string maliciousName, string legitimateName)
    {
        // Act
        var result = await _detector.AnalyzePackageAsync(maliciousName);

        // Assert
        Assert.True(result.IsThreatDetected);
        Assert.Equal(ThreatType.Typosquatting, result.ThreatType);
        Assert.Equal(ThreatSeverity.High, result.Severity);
        Assert.Contains(legitimateName, result.Description);
    }

    [Fact]
    public async Task AnalyzePackageAsync_CompletelyDifferentName_ReturnsNoThreat()
    {
        // Arrange
        var packageName = "my-unique-package-12345";

        // Act
        var result = await _detector.AnalyzePackageAsync(packageName);

        // Assert
        Assert.False(result.IsThreatDetected);
    }
}
