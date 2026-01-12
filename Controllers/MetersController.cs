using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;
using KlaipedosVandenysDemo.Models;

namespace KlaipedosVandenysDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetersController : ControllerBase
{
    private readonly AppDbContext _db;
    public MetersController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<Meter>> Create(Meter meter)
    {
        meter.LastUpdated = DateTime.Now;
        _db.Meters.Add(meter);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = meter.Id }, meter);
    }

    // Helper for Lithuanian number-to-words (reuse from Program.cs)
    private static string NumberToLtWords(decimal amount)
    {
        string[] units = { "nulis", "vienas", "du", "trys", "keturi", "penki", "šeši", "septyni", "aštuoni", "devyni" };
        string[] teens = { "dešimt", "vienuolika", "dvylika", "trylika", "keturiolika", "penkiolika", "šešiolika", "septyniolika", "aštuoniolika", "devyniolika" };
        string[] tens = { "", "dešimt", "dvidešimt", "trisdešimt", "keturiasdešimt", "penkiasdešimt", "šešiasdešimt", "septyniasdešimt", "aštuoniasdešimt", "devyniasdešimt" };
        string[] hundreds = { "", "šimtas", "du šimtai", "trys šimtai", "keturi šimtai", "penki šimtai", "šeši šimtai", "septyni šimtai", "aštuoni šimtai", "devyni šimtai" };

        int euros = (int)amount;
        int cents = (int)((amount - euros) * 100);

        string ToWords(int n)
        {
            if (n == 0) return units[0];
            var parts = new List<string>();
            if (n >= 100)
            {
                parts.Add(hundreds[n / 100]);
                n %= 100;
            }
            if (n >= 20)
            {
                parts.Add(tens[n / 10]);
                n %= 10;
            }
            if (n >= 10)
            {
                parts.Add(teens[n - 10]);
                n = 0;
            }
            if (n > 0)
            {
                parts.Add(units[n]);
            }
            return string.Join(" ", parts);
        }

        string euroWord = euros == 1 ? "euras" : (euros % 10 >= 2 && euros % 10 <= 9 && (euros % 100 < 10 || euros % 100 >= 20) ? "eurai" : "eurų");
        string centWord = cents == 1 ? "centas" : (cents % 10 >= 2 && cents % 10 <= 9 && (cents % 100 < 10 || cents % 100 >= 20) ? "centai" : "centų");

        string result = "";
        if (euros > 0)
            result += ToWords(euros) + " " + euroWord;
        if (cents > 0)
            result += (result.Length > 0 ? " " : "") + ToWords(cents) + " " + centWord;
        if (result == "")
            result = "nulis eurų";
        return result.Trim();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var meters = await _db.Meters.AsNoTracking().ToListAsync();
        return meters.Select(m => new {
            m.Id,
            m.MeterType,
            Value = m.Value.ToString("F2"),
            m.UserId,
            ValueWords = NumberToLtWords(decimal.Round(m.Value, 2))
        }).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var meter = await _db.Meters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (meter is null) return NotFound();
        return Ok(new {
            meter.Id,
            meter.MeterType,
            Value = meter.Value.ToString("F2"),
            meter.UserId,
            ValueWords = NumberToLtWords(decimal.Round(meter.Value, 2))
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Meter>> Update(int id, Meter input)
    {
        var meter = await _db.Meters.FirstOrDefaultAsync(m => m.Id == id);
        if (meter is null) return NotFound();
        meter.MeterType = input.MeterType;
        meter.Value = input.Value;
        meter.UserId = input.UserId;
        meter.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(meter);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var meter = await _db.Meters.FirstOrDefaultAsync(m => m.Id == id);
        if (meter is null) return NotFound();
        _db.Meters.Remove(meter);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("by-user/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByUser(string userId)
    {
        var meters = await _db.Meters.AsNoTracking().Where(m => m.UserId == userId).ToListAsync();
        return meters.Select(m => new {
            m.Id,
            m.MeterType,
            Value = m.Value.ToString("F2"),
            m.UserId,
            m.LastUpdated,
            ValueWords = NumberToLtWords(decimal.Round(m.Value, 2))
        }).ToList();
    }

    [HttpGet("by-user-type")]
    public async Task<ActionResult<object>> GetByUserAndType([FromQuery] string userId, [FromQuery] string type)
    {
        var meter = await _db.Meters.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId && m.MeterType == type);
        if (meter is null) return NotFound();
        return Ok(new {
            meter.Id,
            meter.MeterType,
            Value = meter.Value.ToString("F2"),
            meter.UserId,
            meter.LastUpdated,
            ValueWords = NumberToLtWords(decimal.Round(meter.Value, 2))
        });
    }
}
