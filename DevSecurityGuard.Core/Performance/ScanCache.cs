using System.Collections.Concurrent;

namespace DevSecurityGuard.Core.Performance;

/// <summary>
/// Phase 6: Performance - Caching layer for scan results
/// </summary>
public class ScanCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly int _ttlSeconds;

    public ScanCache(int ttlSeconds = 3600)
    {
        _ttlSeconds = ttlSeconds;
    }

    public void Set(string key, object value)
    {
        _cache[key] = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_ttlSeconds)
        };
    }

    public T? Get<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpiresAt)
            {
                return entry.Value as T;
            }
            
            // Expired, remove
            _cache.TryRemove(key, out _);
        }

        return null;
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public void RemoveExpired()
    {
        var now = DateTime.UtcNow;
        var expired = _cache.Where(kvp => kvp.Value.ExpiresAt < now).Select(kvp => kvp.Key).ToList();
        
        foreach (var key in expired)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}

/// <summary>
/// Phase 6: Performance - Parallel package scanner
/// </summary>
public class ParallelScanner
{
    private readonly int _maxConcurrency;

    public ParallelScanner(int maxConcurrency = 4)
    {
        _maxConcurrency = maxConcurrency;
    }

    public async Task<List<T>> ScanAsync<T>(
        IEnumerable<string> packages,
        Func<string, Task<T>> scanFunc)
    {
        var semaphore = new SemaphoreSlim(_maxConcurrency);
        var tasks = new List<Task<T>>();

        foreach (var package in packages)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await scanFunc(package);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        return (await Task.WhenAll(tasks)).ToList();
    }
}
