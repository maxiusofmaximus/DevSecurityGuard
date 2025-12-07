using Spectre.Console;
using DevSecurityGuard.PluginSystem;

namespace DevSecurityGuard.CLI.Commands;

public static class PluginCommand
{
    private static readonly string PluginsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DevSecurityGuard",
        "plugins");

    public static async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            await ExecuteListAsync();
            return 0;
        }

        var action = args[0].ToLower();

        return action switch
        {
            "list" => await ExecuteListAsync(),
            "info" => await ExecuteInfoAsync(args.Skip(1).FirstOrDefault()),
            _ => ShowPluginHelp()
        };
    }

    private static async Task<int> ExecuteListAsync()
    {
        AnsiConsole.MarkupLine($"[bold]Plugin Directory:[/] {PluginsPath}");
        AnsiConsole.WriteLine();

        if (!Directory.Exists(PluginsPath))
        {
            Directory.CreateDirectory(PluginsPath);
            AnsiConsole.MarkupLine("[yellow]No plugins found[/]");
            AnsiConsole.MarkupLine($"[dim]Place plugins in: {PluginsPath}[/]");
            return 0;
        }

        var registry = new PluginRegistry(PluginsPath);
        await registry.InitializeAsync();

        var plugins = registry.GetAvailablePlugins().ToList();

        if (plugins.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No plugins found[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Version");
        table.AddColumn("Type");
        table.AddColumn("Author");

        foreach (var plugin in plugins)
        {
            table.AddRow(
                plugin.Id,
                plugin.Name,
                plugin.Version,
                plugin.Type.ToString(),
                plugin.Author);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total: {plugins.Count} plugin(s)[/]");
        return 0;
    }

    private static async Task<int> ExecuteInfoAsync(string? id)
    {
        if (id == null)
        {
            AnsiConsole.MarkupLine("[red]Usage:[/] dsg plugin info <id>");
            return 1;
        }

        var registry = new PluginRegistry(PluginsPath);
        await registry.InitializeAsync();

        var plugin = registry.GetAvailablePlugins().FirstOrDefault(p => p.Id == id);

        if (plugin == null)
        {
            AnsiConsole.MarkupLine($"[red]Plugin not found:[/] {id}");
            return 1;
        }

        var panel = new Panel(new Markup($@"
[bold]{plugin.Name}[/]
[dim]ID:[/] {plugin.Id}
[dim]Version:[/] {plugin.Version}
[dim]Author:[/] {plugin.Author}
[dim]Type:[/] {plugin.Type}

{plugin.Description}

[dim]Assembly:[/] {plugin.AssemblyPath}
[dim]Entry Point:[/] {plugin.EntryPoint}
"));

        panel.Header = new PanelHeader($"[blue]Plugin: {plugin.Name}[/]");
        AnsiConsole.Write(panel);
        return 0;
    }

    private static int ShowPluginHelp()
    {
        AnsiConsole.MarkupLine("[bold]Plugin Commands:[/]");
        AnsiConsole.MarkupLine("  dsg plugin list");
        AnsiConsole.MarkupLine("  dsg plugin info <id>");
        return 0;
    }
}
