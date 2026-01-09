namespace KlaipedosVandenysDemo.Models;

public class Bill
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
