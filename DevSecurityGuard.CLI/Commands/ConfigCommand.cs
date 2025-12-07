using System.Text.Json;
using Spectre.Console;

namespace DevSecurityGuard.CLI.Commands;

public static class ConfigCommand
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DevSecurityGuard",
        "config.json");

    public static int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            ExecuteList();
            return 0;
        }

        var action = args[0].ToLower();

        return action switch
        {
            "get" => ExecuteGet(args.Skip(1).FirstOrDefault()),
            "set" => ExecuteSet(args.Skip(1).ToArray()),
            "list" => ExecuteList(),
            _ => ShowConfigHelp()
        };
    }

    private static int ExecuteGet(string? key)
    {
        if (key == null)
        {
            ExecuteList();
            return 0;
        }

        var config = LoadConfig();

        if (config.TryGetValue(key, out var value))
        {
            AnsiConsole.MarkupLine($"[green]{key}[/] = {value}");
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Key not found:[/] {key}");
        return 1;
    }

    private static int ExecuteSet(string[] args)
    {
        if (args.Length < 2)
        {
            AnsiConsole.MarkupLine("[red]Usage:[/] dsg config set <key> <value>");
            return 1;
        }

        var key = args[0];
        var value = args[1];

        var config = LoadConfig();
        config[key] = value;
        SaveConfig(config);

        AnsiConsole.MarkupLine($"[green]✓ Set[/] {key} = {value}");
        return 0;
    }

    private static int ExecuteList()
    {
        var config = LoadConfig();

        if (config.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No configuration found[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Key");
        table.AddColumn("Value");

        foreach (var (key, value) in config)
        {
            table.AddRow(key, value);
        }

        AnsiConsole.Write(table);
        return 0;
    }

    private static int ShowConfigHelp()
    {
        AnsiConsole.MarkupLine("[bold]Config Commands:[/]");
        AnsiConsole.MarkupLine("  dsg config get <key>");
        AnsiConsole.MarkupLine("  dsg config set <key> <value>");
        AnsiConsole.MarkupLine("  dsg config list");
        return 0;
    }

    private static Dictionary<string, string> LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            return new Dictionary<string, string>
            {
                { "lang", "en" },
                { "interventionMode", "interactive" },
                { "forcePnpm", "true" }
            };
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static void SaveConfig(Dictionary<string, string> config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
