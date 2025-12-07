namespace DevSecurityGuard.PluginSystem;

/// <summary>
/// Manages plugin registry and lifecycle
/// </summary>
public class PluginRegistry
{
    private readonly PluginLoader _loader;
    private readonly Dictionary<string, PluginManifest> _availablePlugins = new();
    private readonly List<IDetectorPlugin> _detectorPlugins = new();
    private readonly List<IPackageManagerPlugin> _packageManagerPlugins = new();
    
    public PluginRegistry(string pluginsDirectory)
    {
        _loader = new PluginLoader(pluginsDirectory);
    }
    
    /// <summary>
    /// Initialize the registry and discover plugins
    /// </summary>
    public async Task InitializeAsync()
    {
        var manifests = await _loader.DiscoverPluginsAsync();
        
        foreach (var manifest in manifests)
        {
            _availablePlugins[manifest.Id] = manifest;
        }
        
        Console.WriteLine($"Discovered {_availablePlugins.Count} plugins");
    }
    
    /// <summary>
    /// Load a specific plugin by ID
    /// </summary>
    public async Task<bool> LoadPluginAsync(string pluginId)
    {
        if (!_availablePlugins.TryGetValue(pluginId, out var manifest))
        {
            Console.WriteLine($"Plugin not found: {pluginId}");
            return false;
        }
        
        var plugin = await _loader.LoadPluginAsync(manifest);
        if (plugin == null)
            return false;
        
        // Register based on type
        if (plugin is IDetectorPlugin detector)
        {
            _detectorPlugins.Add(detector);
            _detectorPlugins.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // Sort by priority
        }
        else if (plugin is IPackageManagerPlugin packageManager)
        {
            _packageManagerPlugins.Add(packageManager);
        }
        
        return true;
    }
    
    /// <summary>
    /// Load all available plugins
    /// </summary>
    public async Task LoadAllPluginsAsync()
    {
        foreach (var pluginId in _availablePlugins.Keys)
        {
            await LoadPluginAsync(pluginId);
        }
    }
    
    /// <summary>
    /// Unload a plugin
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        var success = await _loader.UnloadPluginAsync(pluginId);
        
        if (success)
        {
            // Remove from typed lists
            _detectorPlugins.RemoveAll(p => p.Id == pluginId);
            _packageManagerPlugins.RemoveAll(p => p.Id == pluginId);
        }
        
        return success;
    }
    
    /// <summary>
    /// Get all detector plugins
    /// </summary>
    public IEnumerable<IDetectorPlugin> GetDetectorPlugins()
    {
        return _detectorPlugins;
    }
    
    /// <summary>
    /// Get detector plugins for a specific package manager
    /// </summary>
    public IEnumerable<IDetectorPlugin> GetDetectorPluginsForPackageManager(string packageManager)
    {
        return _detectorPlugins.Where(d => 
            d.SupportedPackageManagers.Contains("*") || 
            d.SupportedPackageManagers.Contains(packageManager, StringComparer.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Get all package manager plugins
    /// </summary>
    public IEnumerable<IPackageManagerPlugin> GetPackageManagerPlugins()
    {
        return _packageManagerPlugins;
    }
    
    /// <summary>
    /// Get available (not yet loaded) plugins
    /// </summary>
    public IEnumerable<PluginManifest> GetAvailablePlugins()
    {
        return _availablePlugins.Values;
    }
    
    /// <summary>
    /// Check if a plugin is loaded
    /// </summary>
    public bool IsPluginLoaded(string pluginId)
    {
        return _detectorPlugins.Any(p => p.Id == pluginId) || 
               _packageManagerPlugins.Any(p => p.Id == pluginId);
    }
}
