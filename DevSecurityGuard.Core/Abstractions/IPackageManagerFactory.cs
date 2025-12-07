namespace DevSecurityGuard.Core.Abstractions;

/// <summary>
/// Factory for creating package manager instances
/// </summary>
public interface IPackageManagerFactory
{
    /// <summary>
    /// Get all registered package managers
    /// </summary>
    IEnumerable<IPackageManager> GetAllPackageManagers();
    
    /// <summary>
    /// Get package manager by name
    /// </summary>
    IPackageManager? GetPackageManager(string name);
    
    /// <summary>
    /// Detect which package managers are used in a project
    /// </summary>
    IEnumerable<IPackageManager> DetectPackageManagers(string projectPath);
    
    /// <summary>
    /// Register a new package manager (for plugins)
    /// </summary>
    void RegisterPackageManager(IPackageManager packageManager);
}

/// <summary>
/// Package manager factory implementation
/// </summary>
public class PackageManagerFactory : IPackageManagerFactory
{
    private readonly Dictionary<string, IPackageManager> _packageManagers = new();
    
    public PackageManagerFactory(IEnumerable<IPackageManager> packageManagers)
    {
        foreach (var pm in packageManagers)
        {
            _packageManagers[pm.Name.ToLowerInvariant()] = pm;
        }
    }
    
    public IEnumerable<IPackageManager> GetAllPackageManagers()
    {
        return _packageManagers.Values;
    }
    
    public IPackageManager? GetPackageManager(string name)
    {
        _packageManagers.TryGetValue(name.ToLowerInvariant(), out var pm);
        return pm;
    }
    
    public IEnumerable<IPackageManager> DetectPackageManagers(string projectPath)
    {
        var detected = new List<IPackageManager>();
        
        foreach (var pm in _packageManagers.Values)
        {
            if (pm.IsDetected(projectPath))
            {
                detected.Add(pm);
            }
        }
        
        return detected;
    }
    
    public void RegisterPackageManager(IPackageManager packageManager)
    {
        _packageManagers[packageManager.Name.ToLowerInvariant()] = packageManager;
    }
}
