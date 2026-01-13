namespace KlaipedosVandenysDemo.Models;

public class UserIdentifier
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // For demo simplicity we keep this as a string.
    // Suggested values: personal_code | contract_number | object_number
    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public User? User { get; set; }
}
