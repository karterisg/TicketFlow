namespace TicketFlow.Shared.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public string Role { get; set; } = "Customer";
    public string? AppUserId { get; set; }
}

public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}