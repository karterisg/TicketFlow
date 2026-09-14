namespace TicketFlow.Shared.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TicketCount { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
}