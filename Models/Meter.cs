namespace KlaipedosVandenysDemo.Models;

public class Meter
{
    public int Id { get; set; }
    public string MeterType { get; set; } = string.Empty; // e.g., "hot_water", "cold_water"
    public float Value { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
