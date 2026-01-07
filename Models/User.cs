namespace KlaipedosVandenysDemo.Models;

public class User
{
    public int Id { get; set; }
    public long PersonalCode { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
