using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Authorization;

public static class PermissionCatalog
{
    public static readonly (string Key, string Category, string Description)[] All =
    [
        ("Tickets.ViewAll", "Tickets", "View all tickets, not just their own"),
        ("Tickets.Assign", "Tickets", "Assign a ticket to an agent"),
        ("Tickets.Close", "Tickets", "Close a ticket"),
        ("Tickets.Delete", "Tickets", "Delete a ticket"),
        ("Tickets.Export", "Tickets", "Export tickets to Excel"),
        ("Agents.Manage", "Agents", "Create and manage agents"),
        ("Categories.Manage", "Categories", "Create, edit and delete ticket categories"),
        ("Projects.Manage", "Projects", "Create, edit and delete projects"),
        ("ProjectCategories.Manage", "Projects", "Create, edit and delete project categories"),
        ("ProjectTemplates.Manage", "Projects", "Create, edit and delete project templates"),
        ("Teams.Manage", "Teams", "Create, edit and delete teams"),
        ("Users.Manage", "Users", "Create, edit, delete and promote users"),
        ("Reports.View", "Reports", "View reports and analytics"),
        ("Meetings.Manage", "Meetings", "Create and cancel meetings"),
    ];

    public static async Task SeedAsync(AppDbContext db)
    {
        var existingKeys = await db.Permissions.Select(p => p.Key).ToListAsync();
        foreach (var (key, category, description) in All)
        {
            if (!existingKeys.Contains(key))
                db.Permissions.Add(new Permission { Key = key, Category = category, Description = description });
        }
        await db.SaveChangesAsync();
    }
}
