namespace KlaipedosVandenysDemo.Models;

public class User
{
    public int Id { get; set; }
    public string PersonalCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
