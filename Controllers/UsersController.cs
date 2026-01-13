using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;
using KlaipedosVandenysDemo.Models;
using System.Text.RegularExpressions;

namespace KlaipedosVandenysDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    private static readonly Regex PersonalCodeRegex = new("^\\d{11}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ContractNumberRegex = new("^[A-Z]{2}\\d{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ObjectNumberRegex = new("^\\d{7,9}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("resolve/{identifier}")]
    public async Task<ActionResult> Resolve(string identifier)
    {
        var normalized = (identifier ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return BadRequest(new { error = "Empty identifier" });
        }

        // Normalize common formatting: spaces, dashes, and case.
        normalized = normalized.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();

        string? type = null;
        if (PersonalCodeRegex.IsMatch(normalized)) type = "personal_code";
        else if (ContractNumberRegex.IsMatch(normalized)) type = "contract_number";
        else if (ObjectNumberRegex.IsMatch(normalized)) type = "object_number";

        if (type is null)
        {
            return BadRequest(new { error = "Unknown identifier format", value = normalized });
        }

        var user = await _db.UserIdentifiers
            .AsNoTracking()
            .Where(x => x.Type == type && x.Value == normalized)
            .Select(x => x.User)
            .FirstOrDefaultAsync();

        return user is not null
            ? Ok(new { matchedType = type, user })
            : NotFound(new { matchedType = type, value = normalized });
    }

    [HttpGet("by-personal-code/{code}")]
    public async Task<ActionResult> GetByPersonalCode(string code)
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
