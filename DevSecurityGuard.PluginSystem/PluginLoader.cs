using System.Reflection;
using System.Runtime.Loader;

namespace DevSecurityGuard.PluginSystem;

/// <summary>
/// Loads and manages plugins from assemblies
/// </summary>
public class PluginLoader
{
    private readonly string _pluginsDirectory;
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    
    public PluginLoader(string pluginsDirectory)
    {
        _pluginsDirectory = pluginsDirectory;
        
        // Ensure plugins directory exists
        Directory.CreateDirectory(_pluginsDirectory);
    }
    
    /// <summary>
    /// Discover all plugins in the plugins directory
    /// </summary>
    public async Task<IEnumerable<PluginManifest>> DiscoverPluginsAsync()
    {
        var manifests = new List<PluginManifest>();
        
        // Find all plugin.json files
        var manifestFiles = Directory.GetFiles(_pluginsDirectory, "plugin.json", SearchOption.AllDirectories);
        
        foreach (var manifestFile in manifestFiles)
        {
            var manifest = await PluginManifest.LoadFromFileAsync(manifestFile);
            if (manifest != null)
            {
                var (isValid, errors) = manifest.Validate();
                if (isValid)
                {
                    manifests.Add(manifest);
                }
                else
                {
                    Console.WriteLine($"Invalid plugin manifest: {manifestFile}");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }
            }
        }
        
        return manifests;
    }
    
    /// <summary>
    /// Load a plugin from its manifest
    /// </summary>
    public async Task<IPlugin?> LoadPluginAsync(PluginManifest manifest)
    {
        try
        {
            // Check if already loaded
            if (_loadedPlugins.ContainsKey(manifest.Id))
            {
                return _loadedPlugins[manifest.Id].Instance;
            }
            
            // Resolve assembly path (relative to manifest directory)
            var manifestDir = Path.GetDirectoryName(Path.Combine(_pluginsDirectory, manifest.AssemblyPath)) ?? _pluginsDirectory;
            var assemblyPath = Path.Combine(manifestDir, manifest.AssemblyPath);
            
            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine($"Plugin assembly not found: {assemblyPath}");
                return null;
            }
            
            // Load assembly
            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath)));
            
            // Find and instantiate entry point
            var entryPointType = assembly.GetType(manifest.EntryPoint);
            if (entryPointType == null)
            {
                Console.WriteLine($"Entry point not found: {manifest.EntryPoint}");
                return null;
            }
            
            // Create instance
            var instance = Activator.CreateInstance(entryPointType) as IPlugin;
            if (instance == null)
            {
                Console.WriteLine($"Entry point does not implement IPlugin: {manifest.EntryPoint}");
                return null;
            }
            
            // Initialize plugin
            await instance.InitializeAsync();
            
            // Store loaded plugin
            _loadedPlugins[manifest.Id] = new LoadedPlugin
            {
                Manifest = manifest,
                Instance = instance,
                LoadContext = loadContext
            };
            
            Console.WriteLine($"Loaded plugin: {manifest.Name} v{manifest.Version}");
            
            return instance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load plugin {manifest.Id}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Unload a plugin
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
            return false;
        
        try
        {
            // Dispose plugin
            await loadedPlugin.Instance.DisposeAsync();
            
            // Unload assembly context
            loadedPlugin.LoadContext.Unload();
            
            _loadedPlugins.Remove(pluginId);
            
            Console.WriteLine($"Unloaded plugin: {pluginId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to unload plugin {pluginId}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IEnumerable<IPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.Select(p => p.Instance);
    }
    
    /// <summary>
    /// Get loaded plugins of a specific type
    /// </summary>
    public IEnumerable<T> GetLoadedPlugins<T>() where T : IPlugin
    {
        return _loadedPlugins.Values
            .Select(p => p.Instance)
            .OfType<T>();
    }
}

/// <summary>
/// Custom assembly load context for plugin isolation
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    
    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }
    
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        
        return null;
    }
    
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        
        return IntPtr.Zero;
    }
}

/// <summary>
/// Loaded plugin information
/// </summary>
internal class LoadedPlugin
{
    public PluginManifest Manifest { get; set; } = null!;
    public IPlugin Instance { get; set; } = null!;
    public PluginLoadContext LoadContext { get; set; } = null!;
}
