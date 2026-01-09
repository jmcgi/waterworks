namespace KlaipedosVandenysDemo.Models;

public class Bill
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
