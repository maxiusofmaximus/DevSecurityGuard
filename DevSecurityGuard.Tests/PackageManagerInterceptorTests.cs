using DevSecurityGuard.Service;
using Xunit;

namespace DevSecurityGuard.Tests.UnitTests;

public class PackageManagerInterceptorTests
{
    [Theory]
    [InlineData("npm install lodash", "npm", "install", new[] { "lodash" })]
    [InlineData("yarn add react redux", "yarn", "add", new[] { "react", "redux" })]
    [InlineData("pnpm i typescript --save-dev", "pnpm", "i", new[] { "typescript" })]
    public void ParseCommand_ValidCommand_ExtractsCorrectInfo(
        string command,
        string expectedPm,
        string expectedSubcommand,
        string[] expectedPackages)
    {
        // Act
        var result = PackageManagerInterceptor.ParseCommand(command);

        // Assert
        Assert.Equal(expectedPm, result.PackageManager);
        Assert.Equal(expectedSubcommand, result.Subcommand);
        Assert.Equal(expectedPackages, result.PackageNames.ToArray());
    }

    [Theory]
    [InlineData("npm --version")]
    [InlineData("yarn config list")]
    [InlineData("pnpm store status")]
    public void ParseCommand_NonInstallCommand_ReturnsEmptyPackages(string command)
    {
        // Act
        var result = PackageManagerInterceptor.ParseCommand(command);

        // Assert
        Assert.Empty(result.PackageNames);
    }
}
