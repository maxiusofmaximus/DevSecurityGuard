using DevSecurityGuard.Service.Database;
using DevSecurityGuard.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace DevSecurityGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly DevSecurityDbContext _db;
    private readonly IHubContext<DevSecurityHub> _hubContext;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        DevSecurityDbContext db,
        IHubContext<DevSecurityHub> hubContext,
        ILogger<ConfigController> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all configuration settings
    /// </summary>
    [HttpGet]
    public IActionResult GetConfig()
    {
        var config = _db.Configuration.ToDictionary(c => c.Key, c => c.Value);
        return Ok(config);
    }

    /// <summary>
    /// Get a specific configuration value
    /// </summary>
    [HttpGet("{key}")]
    public IActionResult GetConfigValue(string key)
    {
        var config = _db.Configuration.FirstOrDefault(c => c.Key == key);
        if (config == null)
            return NotFound();

        return Ok(new { key = config.Key, value = config.Value });
    }

    /// <summary>
    /// Update configuration setting
    /// </summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateConfig(string key, [FromBody] ConfigUpdateRequest request)
    {
        var config = _db.Configuration.FirstOrDefault(c => c.Key == key);
        
        if (config == null)
        {
            config = new Service.Models.ConfigurationEntry
            {
                Key = key,
                Value = request.Value
            };
            _db.Configuration.Add(config);
        }
        else
        {
            config.Value = request.Value;
        }

        await _db.SaveChangesAsync();

        // Notify all connected clients
        await _hubContext.Clients.All.SendAsync("ConfigUpdated", new { key, value = request.Value });

        _logger.LogInformation("Config updated: {Key} = {Value}", key, request.Value);

        return Ok(new { key, value = request.Value });
    }

    /// <summary>
    /// Update multiple configuration settings at once
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> UpdateConfigBatch([FromBody] Dictionary<string, string> updates)
    {
        foreach (var (key, value) in updates)
        {
            var config = _db.Configuration.FirstOrDefault(c => c.Key == key);
            
            if (config == null)
            {
                config = new Service.Models.ConfigurationEntry { Key = key, Value = value };
                _db.Configuration.Add(config);
            }
            else
            {
                config.Value = value;
            }
        }

        await _db.SaveChangesAsync();

        // Notify all connected clients
        await _hubContext.Clients.All.SendAsync("ConfigUpdated", updates);

        _logger.LogInformation("Batch config update: {Count} settings", updates.Count);

        return Ok(updates);
    }
}

public record ConfigUpdateRequest(string Value);
