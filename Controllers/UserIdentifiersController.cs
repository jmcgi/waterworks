using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;
using KlaipedosVandenysDemo.Models;

namespace KlaipedosVandenysDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserIdentifiersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserIdentifiersController(AppDbContext db)
    {
        _db = db;
    }

    public record CreateUserIdentifierRequest(int UserId, string Type, string Value);

    [HttpPost]
    public async Task<ActionResult<UserIdentifier>> Create(CreateUserIdentifierRequest request)
    {
        var type = (request.Type ?? string.Empty).Trim().ToLowerInvariant();
        var value = (request.Value ?? string.Empty).Trim();

        if (request.UserId <= 0) return BadRequest(new { error = "UserId must be > 0" });
        if (string.IsNullOrWhiteSpace(type)) return BadRequest(new { error = "Type is required" });
        if (string.IsNullOrWhiteSpace(value)) return BadRequest(new { error = "Value is required" });

        // Keep values consistent with resolver normalization.
        value = value.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();

        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == request.UserId);
        if (!exists) return NotFound(new { error = "User not found", userId = request.UserId });

        var entity = new UserIdentifier
        {
            UserId = request.UserId,
            Type = type,
            Value = value
        };

        _db.UserIdentifiers.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserIdentifier>> GetById(int id)
    {
        var item = await _db.UserIdentifiers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserIdentifier>>> List([FromQuery] int? userId)
    {
        var query = _db.UserIdentifiers.AsNoTracking();
        if (userId is not null) query = query.Where(x => x.UserId == userId);
        var items = await query.OrderByDescending(x => x.Id).ToListAsync();
        return Ok(items);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.UserIdentifiers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return NotFound();

        _db.UserIdentifiers.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
