using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Exceptions;
using TicketFlow.Shared.Domain;
using Xunit;

namespace TicketFlow.Tests.Services;

public class TicketServiceEdgeCaseTests : TicketServiceTestBase
{
   
    // CreateTicketAsync Edge Cases

    [Fact]
    public async Task CreateTicketAsync_MinimumTitleLength_Succeeds()
    {
        // A title with exactly 5 characters (the minimum) should be accepted
        var (service, db) = BuildService(nameof(CreateTicketAsync_MinimumTitleLength_Succeeds));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var ticket = await service.CreateTicketAsync(
            title: "12345",
            description: "Valid description here.",
            userId: user.Id,
            categoryId: category.Id);

        Assert.NotNull(ticket);
        Assert.Equal("12345", ticket.Title);
    }

    [Fact]
    public async Task CreateTicketAsync_MaximumTitleLength_Succeeds()
    {
        // A title with exactly 200 characters (the maximum) should be accepted
        var (service, db) = BuildService(nameof(CreateTicketAsync_MaximumTitleLength_Succeeds));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var longTitle = new string('A', 200);
        var ticket = await service.CreateTicketAsync(
            title: longTitle,
            description: "Valid description here.",
            userId: user.Id,
            categoryId: category.Id);

        Assert.NotNull(ticket);
        Assert.Equal(200, ticket.Title.Length);
    }

    [Fact]
    public async Task CreateTicketAsync_DuplicateTitle_Succeeds()
    {
        // Two tickets with the same title should both be created successfully
        // There is no unique constraint on title
        var (service, db) = BuildService(nameof(CreateTicketAsync_DuplicateTitle_Succeeds));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var t1 = await service.CreateTicketAsync("Same Title", "Desc 1", user.Id, category.Id);
        var t2 = await service.CreateTicketAsync("Same Title", "Desc 2", user.Id, category.Id);

        Assert.NotEqual(t1.Id, t2.Id);    // different DB ids
        Assert.Equal(t1.Title, t2.Title); // same title is allowed
    }

    [Fact]
    public async Task CreateTicketAsync_WithDueDate_SetsDueDate()
    {
        // When a due date is provided it should be persisted on the ticket
        var (service, db) = BuildService(nameof(CreateTicketAsync_WithDueDate_SetsDueDate));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var dueDate = DateTime.UtcNow.AddDays(7);
        var ticket = await service.CreateTicketAsync(
            "Title", "Desc", user.Id, category.Id, dueDate: dueDate);

        Assert.NotNull(ticket.DueDate);
    }

    [Fact]
    public async Task CreateTicketAsync_AllPriorities_ParseCorrectly()
    {
        // Every valid priority string should map to the correct enum value
        var (service, db) = BuildService(nameof(CreateTicketAsync_AllPriorities_ParseCorrectly));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var low = await service.CreateTicketAsync("T1", "D", user.Id, category.Id, "Low");
        var medium = await service.CreateTicketAsync("T2", "D", user.Id, category.Id, "Medium");
        var high = await service.CreateTicketAsync("T3", "D", user.Id, category.Id, "High");

        Assert.Equal(TicketPriority.Low, low.Priority);
        Assert.Equal(TicketPriority.Medium, medium.Priority);
        Assert.Equal(TicketPriority.High, high.Priority);
    }

    // AssignTicketAsync Edge Cases
    

    [Fact]
    public async Task AssignTicketAsync_AlreadyInProgressTicket_ThrowsInvalidOperationException()
    {
        // A ticket that is already InProgress cannot be reassigned to a different agent
        var (service, db) = BuildService(nameof(AssignTicketAsync_AlreadyInProgressTicket_ThrowsInvalidOperationException));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var agent1 = new Agent { FullName = "Agent 1", Email = "a1@a.com", IsAvailable = true };
        var agent2 = new Agent { FullName = "Agent 2", Email = "a2@a.com", IsAvailable = true };
        db.Agents.AddRange(agent1, agent2);
        await db.SaveChangesAsync();

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.AssignTicketAsync(ticket.Id, agent1.Id); // first assign → InProgress

        // Second assign on the same ticket should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignTicketAsync(ticket.Id, agent2.Id));
    }

    [Fact]
    public async Task AssignTicketAsync_ClosedTicket_ThrowsInvalidOperationException()
    {
        // A closed ticket cannot be assigned to an agent
        var (service, db) = BuildService(nameof(AssignTicketAsync_ClosedTicket_ThrowsInvalidOperationException));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var agent = new Agent { FullName = "Agent", Email = "a@a.com", IsAvailable = true };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.CloseTicketAsync(ticket.Id, true); // close first

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignTicketAsync(ticket.Id, agent.Id));
    }

    [Fact]
    public async Task AssignTicketAsync_NonExistentAgent_ThrowsNotFoundException()
    {
        // Assigning to an agentId that does not exist should throw
        var (service, db) = BuildService(nameof(AssignTicketAsync_NonExistentAgent_ThrowsNotFoundException));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AssignTicketAsync(ticket.Id, agentId: 9999));
    }

    // ResolveTicketAsync Edge Cases
   

    [Fact]
    public async Task ResolveTicketAsync_AlreadyResolvedTicket_ThrowsInvalidOperationException()
    {
        // Resolving a ticket that is already Resolved should throw
        var (service, db) = BuildService(nameof(ResolveTicketAsync_AlreadyResolvedTicket_ThrowsInvalidOperationException));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var agent = new Agent { FullName = "Agent", Email = "a@b.com", IsAvailable = true };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.AssignTicketAsync(ticket.Id, agent.Id);
        await service.ResolveTicketAsync(ticket.Id); // first resolve

        // Second resolve should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResolveTicketAsync(ticket.Id));
    }

    [Fact]
    public async Task ResolveTicketAsync_ClosedTicket_ThrowsInvalidOperationException()
    {
        // A closed ticket cannot be resolved
        var (service, db) = BuildService(nameof(ResolveTicketAsync_ClosedTicket_ThrowsInvalidOperationException));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.CloseTicketAsync(ticket.Id, true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResolveTicketAsync(ticket.Id));
    }

    
    // DeleteTicketAsync Edge Cases
    

    [Fact]
    public async Task DeleteTicketAsync_InProgressTicket_SoftDeletesIt()
    {
        // Deleting an InProgress ticket should be allowed (soft delete)
        var (service, db) = BuildService(nameof(DeleteTicketAsync_InProgressTicket_SoftDeletesIt));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var agent = new Agent { FullName = "Agent", Email = "a@a.com", IsAvailable = true };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.AssignTicketAsync(ticket.Id, agent.Id);
        await service.DeleteTicketAsync(ticket.Id);

        // IgnoreQueryFilters bypasses the soft-delete EF query filter
        var deleted = await db.Tickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == ticket.Id);

        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);
    }

    [Fact]
    public async Task DeleteTicketAsync_AlreadyDeletedTicket_ThrowsNotFoundException()
    {
        // Deleting a ticket that was already soft-deleted should throw
        // because the query filter hides it and it looks like it doesn't exist
        var (service, db) = BuildService(nameof(DeleteTicketAsync_AlreadyDeletedTicket_ThrowsNotFoundException));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.DeleteTicketAsync(ticket.Id); // first delete

        // Second delete — ticket is hidden by query filter so service throws
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteTicketAsync(ticket.Id));
    }

 
    // GetAllTicketsAsync Edge Cases
   

    [Fact]
    public async Task GetAllTicketsAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // When there are no tickets, should return an empty collection not null
        var (service, _) = BuildService(nameof(GetAllTicketsAsync_EmptyDatabase_ReturnsEmptyList));

        var all = await service.GetAllTicketsAsync();

        Assert.NotNull(all);
        Assert.Empty(all);
    }

    [Fact]
    public async Task GetAllTicketsAsync_MultipleUsers_ReturnsAllTickets()
    {
        // Tickets belonging to different users should all be returned
        var (service, db) = BuildService(nameof(GetAllTicketsAsync_MultipleUsers_ReturnsAllTickets));

        var user1 = new User { FullName = "User 1", Email = "u1@test.com" };
        var user2 = new User { FullName = "User 2", Email = "u2@test.com" };
        var category = new Category { Name = "Bug" };
        db.Users.AddRange(user1, user2);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        await service.CreateTicketAsync("T1", "D", user1.Id, category.Id);
        await service.CreateTicketAsync("T2", "D", user2.Id, category.Id);
        await service.CreateTicketAsync("T3", "D", user1.Id, category.Id);

        var all = await service.GetAllTicketsAsync();

        Assert.Equal(3, all.Count());
    }
}