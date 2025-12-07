using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DevSecurityGuard.Service;

/// <summary>
/// Monitors system for package manager processes
/// </summary>
public class ProcessMonitor
{
    private readonly ILogger<ProcessMonitor> _logger;
    private readonly PackageManagerInterceptor _interceptor;
    private readonly HashSet<string> _packageManagers;

    public ProcessMonitor(
        ILogger<ProcessMonitor> logger,
        PackageManagerInterceptor interceptor)
    {
        _logger = logger;
        _interceptor = interceptor;
        _packageManagers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "npm", "npm.cmd", "npm.exe",
            "yarn", "yarn.cmd", "yarn.exe",
            "pnpm", "pnpm.cmd", "pnpm.exe"
        };
    }

    /// <summary>
    /// Start monitoring for package manager processes
    /// </summary>
    public void StartMonitoring()
    {
        _logger.LogInformation("Starting process monitor for package managers");

        // In a full implementation, this would use:
        // - WMI (Windows Management Instrumentation) to monitor process creation
        // - Event Tracing for Windows (ETW)
        // - Or a kernel-mode driver for low-level interception

        // For demonstration, this is a placeholder
        _logger.LogInformation("Process monitoring would be active here");
    }

    /// <summary>
    /// Check if a process is a package manager
    /// </summary>
    public bool IsPackageManagerProcess(string processName)
    {
        var baseName = Path.GetFileNameWithoutExtension(processName);
        return _packageManagers.Contains(baseName);
    }

    /// <summary>
    /// Handle detected package manager process
    /// </summary>
    public async Task<bool> HandlePackageManagerProcessAsync(
        string processName,
        string commandLine,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detected package manager process: {ProcessName} with command: {CommandLine}",
            processName, commandLine);

        var commandInfo = PackageManagerInterceptor.ParseCommand(commandLine);

        if (commandInfo.PackageNames.Count == 0)
        {
            _logger.LogDebug("No packages to analyze in command");
            return true; // Allow command to proceed
        }

        var result = await _interceptor.AnalyzeInstallationAsync(
            commandInfo.PackageManager,
            commandLine,
            commandInfo.PackageNames,
            cancellationToken);

        if (result.ShouldBlock)
        {
            _logger.LogWarning("Blocking package manager command due to: {Reason}", result.BlockReason);
            return false; // Block the command
        }

        if (result.ShouldRedirect)
        {
            _logger.LogInformation("Command should be redirected to: {RedirectedCommand}", result.RedirectedCommand);
            // In full implementation, would execute redirected command
        }

        return true; // Allow command
    }
}
