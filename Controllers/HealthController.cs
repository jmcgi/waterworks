using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;

namespace KlaipedosVandenysDemo.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    public HealthController(AppDbContext db) => _db = db;

    [HttpGet("db")]
    public async Task<IActionResult> Db()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            return canConnect ? Ok(new { status = "ok" }) : StatusCode(503, new { status = "unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
}
