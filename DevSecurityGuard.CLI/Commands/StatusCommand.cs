using Spectre.Console;

namespace DevSecurityGuard.CLI.Commands;

public static class StatusCommand
{
    public static int Execute(string[] args)
    {
        var table = new Table();
        table.AddColumn("Component");
        table.AddColumn("Status");
        table.AddColumn("Details");

        // Check API
        var apiStatus = CheckAPI();
        table.AddRow(
            "API Server",
            apiStatus.IsRunning ? "[green]✓ Running[/]" : "[red]✗ Stopped[/]",
            apiStatus.Url);

        // Check Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DevSecurityGuard",
            "devsecurity.db");
        var dbExists = File.Exists(dbPath);
        table.AddRow(
            "Database",
            dbExists ? "[green]✓ Found[/]" : "[yellow]⚠ Not found[/]",
            dbPath);

        // Check Plugins
        var pluginsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevSecurityGuard",
            "plugins");
        var pluginCount = Directory.Exists(pluginsPath) 
            ? Directory.GetDirectories(pluginsPath).Length 
            : 0;
        table.AddRow(
            "Plugins",
            pluginCount > 0 ? $"[green]{pluginCount} loaded[/]" : "[dim]None[/]",
            pluginsPath);

        // Check Package Managers
        table.AddRow(
            "Package Managers",
            "[green]8 supported[/]",
            "npm, pip, cargo, nuget, maven, gradle, gem, composer");

        AnsiConsole.Write(table);
        return 0;
    }

    private static (bool IsRunning, string Url) CheckAPI()
    {
        var url = "http://localhost:5000";
        
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = client.GetAsync($"{url}/api/config").Result;
            return (response.IsSuccessStatusCode, url);
        }
        catch
        {
            return (false, url);
        }
    }
}
