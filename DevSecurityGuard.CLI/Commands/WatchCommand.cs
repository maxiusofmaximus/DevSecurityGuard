using Spectre.Console;

namespace DevSecurityGuard.CLI.Commands;

public static class WatchCommand
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        var path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        
        AnsiConsole.MarkupLine($"[bold]Watching:[/] {path}");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop[/]");
        AnsiConsole.WriteLine();

        using var watcher = new FileSystemWatcher(path);
        watcher.Filter = "*.*";
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        watcher.IncludeSubdirectories = false;

        var relevantFiles = new[] { "package.json", "requirements.txt", "Cargo.toml", "*.csproj", "pom.xml", "build.gradle", "Gemfile", "composer.json" };

        watcher.Changed += (sender, e) =>
        {
            if (e.Name != null && relevantFiles.Any(pattern => IsMatch(e.Name, pattern)))
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] {DateTime.Now:HH:mm:ss} - {e.ChangeType}: {e.Name}");
            }
        };

        watcher.Created += (sender, e) =>
        {
            if (e.Name != null && relevantFiles.Any(pattern => IsMatch(e.Name, pattern)))
            {
                AnsiConsole.MarkupLine($"[green]✓[/] {DateTime.Now:HH:mm:ss} - Created: {e.Name}");
            }
        };

        watcher.Deleted += (sender, e) =>
        {
            if (e.Name != null && relevantFiles.Any(pattern => IsMatch(e.Name, pattern)))
            {
                AnsiConsole.MarkupLine($"[red]✗[/] {DateTime.Now:HH:mm:ss} - Deleted: {e.Name}");
            }
        };

        watcher.EnableRaisingEvents = true;

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Stopped watching[/]");
        }

        return 0;
    }

    private static bool IsMatch(string fileName, string pattern)
    {
        if (pattern.Contains('*'))
        {
            var extension = pattern.Replace("*", "");
            return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }
        
        return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
