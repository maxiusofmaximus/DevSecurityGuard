using DevSecurityGuard.Service.Database;
using DevSecurityGuard.Service.Models;
using DevSecurityGuard.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace DevSecurityGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly DevSecurityDbContext _db;
    private readonly IHubContext<DevSecurityHub> _hubContext;

    public ActivityController(DevSecurityDbContext db, IHubContext<DevSecurityHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Get recent activity/threat entries  
    /// </summary>
    [HttpGet]
    public IActionResult GetActivity([FromQuery] int limit = 50)
    {
        var threats = _db.Threats
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .Select(t => new
            {
                t.Id,
                t.PackageName,
                t.ThreatType,
                t.Severity,
                t.Description,
                DetectedAt = t.Timestamp,
                WasBlocked = t.ActionTaken == ThreatAction.Blocked
            })
            .ToList();

        return Ok(threats);
    }

    /// <summary>
    /// Get statistics
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var stats = new
        {
            threatsBlocked = _db.Threats.Count(t => t.ActionTaken == ThreatAction.Blocked),
            packagesScanned = _db.ScanCache.Count(),
            detectorsActive = 5,
            lastScan = _db.Threats.OrderByDescending(t => t.Timestamp).FirstOrDefault()?.Timestamp
        };

        return Ok(stats);
    }

    /// <summary>
    /// Add new activity entry (called by service)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddActivity([FromBody] ThreatEntry threat)
    {
        _db.Threats.Add(threat);
        await _db.SaveChangesAsync();

        // Notify all connected clients
        await _hubContext.Clients.All.SendAsync("ActivityUpdated", new
        {
            threat.Id,
            threat.PackageName,
            threat.ThreatType,
            threat.Severity,
            threat.Description,
            DetectedAt = threat.Timestamp,
            WasBlocked = threat.ActionTaken == ThreatAction.Blocked
        });

        return CreatedAtAction(nameof(GetActivity), new { id = threat.Id }, threat);
    }
}
