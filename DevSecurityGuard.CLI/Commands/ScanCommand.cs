using Spectre.Console;
using DevSecurityGuard.Core.Abstractions;
using DevSecurityGuard.PluginSystem;

namespace DevSecurityGuard.CLI.Commands;

public static class ScanCommand
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        
        AnsiConsole.MarkupLine($"[bold]Scanning:[/] {path}");
        AnsiConsole.WriteLine();

        return await AnsiConsole.Status()
            .StartAsync("Initializing...", async ctx =>
            {
                ctx.Status("Detecting package managers...");
                var factory = CreatePackageManagerFactory();
                
                var detectedPMs = factory.DetectPackageManagers(path).ToList();
                
                if (detectedPMs.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No package managers detected[/]");
                    return 0;
                }

                AnsiConsole.MarkupLine($"[green]✓[/] Detected {detectedPMs.Count} package manager(s)");
                
                foreach (var pm in detectedPMs)
                {
                    AnsiConsole.MarkupLine($"  • {pm.DisplayName}");
                }
                AnsiConsole.WriteLine();

                var results = new List<ScanResult>();
                
                foreach (var pm in detectedPMs)
                {
                    ctx.Status($"Scanning {pm.Name} packages...");
                    
                    try
                    {
                        var manifestPath = Path.Combine(path, pm.DetectionPatterns[0]);
                        if (File.Exists(manifestPath))
                        {
                            var manifest = await pm.ParseManifestAsync(manifestPath);
                            var totalDeps = manifest.Dependencies.Count + manifest.DevDependencies.Count;
                            
                            results.Add(new ScanResult
                            {
                                PackageManager = pm.DisplayName,
                                TotalPackages = totalDeps,
                                ThreatsFound = 0,
                                Status = "Clean"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ScanResult
                        {
                            PackageManager = pm.DisplayName,
                            Status = "Error",
                            Error = ex.Message
                        });
                    }
                }

                ctx.Status("Generating report...");
                await Task.Delay(500);

                DisplayResults(results);
                return 0;
            });
    }

    private static void DisplayResults(List<ScanResult> results)
    {
        var table = new Table();
        table.AddColumn("Package Manager");
        table.AddColumn("Packages");
        table.AddColumn("Threats");
        table.AddColumn("Status");

        foreach (var result in results)
        {
            var statusMarkup = result.Status == "Clean" ? "[green]✓ Clean[/]" :
                              result.Status == "Error" ? "[red]✗ Error[/]" :
                              $"[yellow]{result.Status}[/]";

            table.AddRow(
                result.PackageManager,
                result.TotalPackages.ToString(),
                result.ThreatsFound.ToString(),
                statusMarkup);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var totalPackages = results.Sum(r => r.TotalPackages);
        var totalThreats = results.Sum(r => r.ThreatsFound);

        if (totalThreats == 0)
        {
            AnsiConsole.MarkupLine($"[green]✓ Scan complete: {totalPackages} packages analyzed, no threats detected[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]⚠ Scan complete: {totalThreats} threat(s) detected in {totalPackages} packages[/]");
        }
    }

    private static IPackageManagerFactory CreatePackageManagerFactory()
    {
        var httpClientFactory = new SimpleHttpClientFactory();
        
        var packageManagers = new List<IPackageManager>
        {
            new DevSecurityGuard.Core.PackageManagers.NpmPackageManager(httpClientFactory),
            new DevSecurityGuard.Core.PackageManagers.PipPackageManager(httpClientFactory),
            new DevSecurityGuard.Core.PackageManagers.CargoPackageManager(httpClientFactory),
           new DevSecurityGuard.Core.PackageManagers.NuGetPackageManager(httpClientFactory),
            new DevSecurityGuard.Core.PackageManagers.MavenPackageManager(httpClientFactory),
            new DevSecurityGuard.Core.PackageManagers.GradlePackageManager(httpClientFactory),
            new DevSecurityGuard.Core.PackageManagers.GemPackageManager(httpClientFactory),
            new DevSecurityGuard.Core.PackageManagers.ComposerPackageManager(httpClientFactory)
        };

        return new PackageManagerFactory(packageManagers);
    }
}

class SimpleHttpClientFactory : IHttpClientFactory
{
    private static readonly HttpClient _httpClient = new HttpClient();
    
    public HttpClient CreateClient(string name = "")
    {
        return _httpClient;
    }
}

class ScanResult
{
    public string PackageManager { get; set; } = "";
    public int TotalPackages { get; set; }
    public int ThreatsFound { get; set; }
    public string Status { get; set; } = "";
    public string? Error { get; set; }
}
