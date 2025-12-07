using Spectre.Console;
using DevSecurityGuard.Core.Abstractions;
using DevSecurityGuard.PluginSystem;

namespace DevSecurityGuard.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        ShowBanner();

        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        try
        {
            var command = args[0].ToLower();
            var remaining = args.Skip(1).ToArray();

            return command switch
            {
                "scan" => await Commands.ScanCommand.ExecuteAsync(remaining),
                "watch" => await Commands.WatchCommand.ExecuteAsync(remaining),
                "config" => Commands.ConfigCommand.Execute(remaining),
                "plugin" => await Commands.PluginCommand.ExecuteAsync(remaining),
                "status" => Commands.StatusCommand.Execute(remaining),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => ShowInvalidCommand(command)
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    static void ShowBanner()
    {
        AnsiConsole.Write(
            new FigletText("DevSecurityGuard")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[dim]Multi-Package-Manager Security Scanner v2.0[/]");
        AnsiConsole.WriteLine();
    }

    static int ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]Usage:[/] dsg <command> [options]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Command");
        table.AddColumn("Description");

        table.AddRow("scan", "Scan current directory for threats");
        table.AddRow("watch", "Watch directory for package manager activity");
        table.AddRow("config", "Manage configuration");
        table.AddRow("plugin", "Manage plugins");
        table.AddRow("status", "Show system status");
        table.AddRow("help", "Show this help message");

        AnsiConsole.Write(table);
        return 0;
    }

    static int ShowInvalidCommand(string command)
    {
        AnsiConsole.MarkupLine($"[red]Invalid command:[/] {command}");
        AnsiConsole.MarkupLine("Run [green]dsg help[/] for available commands");
        return 1;
    }
}
