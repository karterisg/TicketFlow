using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Exceptions;
using TicketFlow.Shared.Domain;
using Xunit;

namespace TicketFlow.Tests.Services;

/// <summary>
/// Core unit tests for TicketService.
/// Covers happy path and basic error cases using the AAA pattern (Arrange, Act, Assert).
/// </summary>
public class TicketServiceTests : TicketServiceTestBase
{
    // CreateTicketAsync

    [Fact]
    public async Task CreateTicketAsync_ValidData_ReturnsCreatedTicket()
    {
        // Arrange
        var (service, db) = BuildService(nameof(CreateTicketAsync_ValidData_ReturnsCreatedTicket));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        // Act
        var ticket = await service.CreateTicketAsync(
            title: "Login page broken",
            description: "Cannot login.",
            userId: user.Id,
            categoryId: category.Id,
            priority: "High");

        // Assert — ticket is returned with correct values and a valid DB id
        Assert.NotNull(ticket);
        Assert.Equal("Login page broken", ticket.Title);
        Assert.Equal(TicketStatus.Open, ticket.Status);   // new tickets always start as Open
        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.True(ticket.Id > 0);
    }

    [Fact]
    public async Task CreateTicketAsync_InvalidUserId_ThrowsNotFoundException()
    {
        // Arrange
        var (service, db) = BuildService(nameof(CreateTicketAsync_InvalidUserId_ThrowsNotFoundException));
        var (_, category) = await SeedUserAndCategoryAsync(db);

        // Act & Assert — userId 999 does not exist, service must throw
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateTicketAsync("Title", "Desc", userId: 999, categoryId: category.Id));
    }

    [Fact]
    public async Task CreateTicketAsync_InvalidCategoryId_ThrowsNotFoundException()
    {
        // Arrange
        var (service, db) = BuildService(nameof(CreateTicketAsync_InvalidCategoryId_ThrowsNotFoundException));
        var (user, _) = await SeedUserAndCategoryAsync(db);

        // Act & Assert — categoryId 999 does not exist, service must throw
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateTicketAsync("Title", "Desc", userId: user.Id, categoryId: 999));
    }

    [Fact]
    public async Task CreateTicketAsync_InvalidPriority_DefaultsToMedium()
    {
        // Arrange
        var (service, db) = BuildService(nameof(CreateTicketAsync_InvalidPriority_DefaultsToMedium));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        // Act — passing a priority string that doesn't map to any enum value
        var ticket = await service.CreateTicketAsync(
            "Title", "Desc", user.Id, category.Id, priority: "GARBAGE");

        // Assert — service should fall back to Medium instead of throwing
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
    }

    // GetTicketByIdAsync

    [Fact]
    public async Task GetTicketByIdAsync_ExistingTicket_ReturnsDto()
    {
        // Arrange
        var (service, db) = BuildService(nameof(GetTicketByIdAsync_ExistingTicket_ReturnsDto));
        var (user, category) = await SeedUserAndCategoryAsync(db);
        var created = await service.CreateTicketAsync("My ticket", "Desc", user.Id, category.Id);

        // Act
        var dto = await service.GetTicketByIdAsync(created.Id);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(created.Id, dto.Id);
        Assert.Equal("My ticket", dto.Title);
    }

    [Fact]
    public async Task GetTicketByIdAsync_NonExistentTicket_ReturnsNull()
    {
        // Arrange
        var (service, _) = BuildService(nameof(GetTicketByIdAsync_NonExistentTicket_ReturnsNull));

        // Act
        var dto = await service.GetTicketByIdAsync(id: 9999);

        // Assert — should return null, not throw
        Assert.Null(dto);
    }

    // GetAllTicketsAsync


    [Fact]
    public async Task GetAllTicketsAsync_ReturnsAllNonDeletedTickets()
    {
        // Arrange
        var (service, db) = BuildService(nameof(GetAllTicketsAsync_ReturnsAllNonDeletedTickets));
        var (user, category) = await SeedUserAndCategoryAsync(db);
        await service.CreateTicketAsync("Ticket A", "Desc", user.Id, category.Id);
        await service.CreateTicketAsync("Ticket B", "Desc", user.Id, category.Id);

        // Act
        var all = await service.GetAllTicketsAsync();

        // Assert — both tickets should be returned
        Assert.Equal(2, all.Count());
    }

    // AssignTicketAsync


    [Fact]
    public async Task AssignTicketAsync_AvailableAgent_SetsStatusInProgress()
    {
        // Arrange
        var (service, db) = BuildService(nameof(AssignTicketAsync_AvailableAgent_SetsStatusInProgress));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var agent = new Agent { FullName = "giorgos", Email = "giorgos@gmail.com", IsAvailable = true };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var ticket = await service.CreateTicketAsync("Need help", "Desc", user.Id, category.Id);

        // Act
        await service.AssignTicketAsync(ticket.Id, agent.Id);

        // Assert — reload from DB to verify the change was persisted
        var updated = await db.Tickets.FindAsync(ticket.Id);
        Assert.Equal(TicketStatus.InProgress, updated!.Status);
        Assert.Equal(agent.Id, updated.AgentId);
    }

    [Fact]
    public async Task AssignTicketAsync_UnavailableAgent_ThrowsBadRequestException()
    {
        // Arrange
        var (service, db) = BuildService(nameof(AssignTicketAsync_UnavailableAgent_ThrowsBadRequestException));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var agent = new Agent { FullName = "Busy Agent", Email = "busy@support.com", IsAvailable = false };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var ticket = await service.CreateTicketAsync("Need help", "Desc", user.Id, category.Id);

        // Act & Assert — cannot assign to an unavailable agent
        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AssignTicketAsync(ticket.Id, agent.Id));
    }

    [Fact]
    public async Task AssignTicketAsync_NonExistentTicket_ThrowsNotFoundException()
    {
        // Arrange
        var (service, db) = BuildService(nameof(AssignTicketAsync_NonExistentTicket_ThrowsNotFoundException));

        var agent = new Agent { FullName = "Some Agent", Email = "a@a.com", IsAvailable = true };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        // Act & Assert — ticket 9999 does not exist
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AssignTicketAsync(ticketId: 9999, agentId: agent.Id));
    }

    
    // ResolveTicketAsync

    [Fact]
    public async Task ResolveTicketAsync_InProgressTicket_SetsStatusResolved()
    {
        // Arrange — ticket must be InProgress before it can be resolved
        var (service, db) = BuildService(nameof(ResolveTicketAsync_InProgressTicket_SetsStatusResolved));
        var (user, category) = await SeedUserAndCategoryAsync(db);

        var agent = new Agent { FullName = "Agent", Email = "a@b.com", IsAvailable = true };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.AssignTicketAsync(ticket.Id, agent.Id);

        // Act
        await service.ResolveTicketAsync(ticket.Id);

        // Assert — status is Resolved and ResolvedAt timestamp is set
        var resolved = await db.Tickets.FindAsync(ticket.Id);
        Assert.Equal(TicketStatus.Resolved, resolved!.Status);
        Assert.NotNull(resolved.ResolvedAt);
    }

    [Fact]
    public async Task ResolveTicketAsync_OpenTicket_ThrowsInvalidOperationException()
    {
        // Arrange — ticket is Open (never assigned), violates business rule
        var (service, db) = BuildService(nameof(ResolveTicketAsync_OpenTicket_ThrowsInvalidOperationException));
        var (user, category) = await SeedUserAndCategoryAsync(db);
        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);

        // Act & Assert — cannot resolve without assigning first
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResolveTicketAsync(ticket.Id));
    }

    // CloseTicketAsync
   

    [Fact]
    public async Task CloseTicketAsync_OpenTicket_SetsStatusClosed()
    {
        // Arrange
        var (service, db) = BuildService(nameof(CloseTicketAsync_OpenTicket_SetsStatusClosed));
        var (user, category) = await SeedUserAndCategoryAsync(db);
        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);

        // Act
        await service.CloseTicketAsync(ticket.Id, true);

        // Assert
        var closed = await db.Tickets.FindAsync(ticket.Id);
        Assert.Equal(TicketStatus.Closed, closed!.Status);
    }

    [Fact]
    public async Task CloseTicketAsync_AlreadyClosedTicket_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, db) = BuildService(nameof(CloseTicketAsync_AlreadyClosedTicket_ThrowsInvalidOperationException));
        var (user, category) = await SeedUserAndCategoryAsync(db);
        var ticket = await service.CreateTicketAsync("Issue", "Desc", user.Id, category.Id);
        await service.CloseTicketAsync(ticket.Id, true); // first close

        // Act & Assert — closing an already closed ticket should throw (domain rule)
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CloseTicketAsync(ticket.Id, true));
    }

    
    // DeleteTicketAsync

    [Fact]
    public async Task DeleteTicketAsync_ExistingTicket_SoftDeletesIt()
    {
        // Arrange
        var (service, db) = BuildService(nameof(DeleteTicketAsync_ExistingTicket_SoftDeletesIt));
        var (user, category) = await SeedUserAndCategoryAsync(db);
        var ticket = await service.CreateTicketAsync("To delete", "Desc", user.Id, category.Id);

        // Act
        await service.DeleteTicketAsync(ticket.Id);

        // Assert — IgnoreQueryFilters needed because the EF query filter hides deleted records
        var deleted = await db.Tickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == ticket.Id);

        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);     // marked as deleted
        Assert.NotNull(deleted.DeletedAt);   // timestamp was set
    }

    [Fact]
    public async Task DeleteTicketAsync_NonExistentTicket_ThrowsNotFoundException()
    {
        // Arrange
        var (service, _) = BuildService(nameof(DeleteTicketAsync_NonExistentTicket_ThrowsNotFoundException));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteTicketAsync(ticketId: 9999));
    }

    [Fact]
    public async Task DeleteTicketAsync_DeletedTicket_NotVisibleInGetAll()
    {
        // Arrange
        var (service, db) = BuildService(nameof(DeleteTicketAsync_DeletedTicket_NotVisibleInGetAll));
        var (user, category) = await SeedUserAndCategoryAsync(db);
        var t1 = await service.CreateTicketAsync("Keep me", "Desc", user.Id, category.Id);
        var t2 = await service.CreateTicketAsync("Delete me", "Desc", user.Id, category.Id);

        // Act
        await service.DeleteTicketAsync(t2.Id);
        var all = await service.GetAllTicketsAsync();

        // Assert — soft-delete query filter means only non-deleted tickets are returned
        Assert.Single(all);
        Assert.Equal(t1.Id, all.First().Id);
    }
}