using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;
using KlaipedosVandenysDemo.Models;

namespace KlaipedosVandenysDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("by-personal-code/{code}")]
    public async Task<ActionResult> GetByPersonalCode(long code)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PersonalCode == code);
            return user is not null ? Ok(user) : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lookup failed", message = ex.Message });
        }
    }

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<User>> GetByEmail(string email)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("by-surname/{surname}")]
    public async Task<ActionResult<IEnumerable<User>>> GetBySurname(string surname)
    {
        var users = await _db.Users.AsNoTracking().Where(u => u.Surname == surname).ToListAsync();
        if (users.Count == 0) return NotFound();
        return Ok(users);
    }

    [HttpGet("by-phone/{phone}")]
    public async Task<ActionResult<IEnumerable<User>>> GetByPhone(string phone)
    {
        var users = await _db.Users.AsNoTracking().Where(u => u.Phone == phone).ToListAsync();
        if (users.Count == 0) return NotFound();
        return Ok(users);
    }
}
