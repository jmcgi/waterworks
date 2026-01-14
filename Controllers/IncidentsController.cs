using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;
using KlaipedosVandenysDemo.Models;

namespace KlaipedosVandenysDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public IncidentsController(AppDbContext db) => _db = db;

    public record SubmitIncidentRequest(int UserId, string Description);

    [HttpPost]
    public async Task<ActionResult<Incident>> Submit(SubmitIncidentRequest request)
    {
        if (request.UserId <= 0) return BadRequest(new { error = "UserId must be > 0" });
        if (string.IsNullOrWhiteSpace(request.Description)) return BadRequest(new { error = "Description required" });

        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == request.UserId);
        if (!exists) return NotFound(new { error = "User not found", userId = request.UserId });

        var incident = new Incident
        {
            UserId = request.UserId,
            Description = request.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            Status = "new"
        };
        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Incident>> GetById(int id)
    {
        var incident = await _db.Incidents.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return incident is null ? NotFound() : Ok(incident);
    }
}
