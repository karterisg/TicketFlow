using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketFlow.API.Data;
using TicketFlow.API.Hubs;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Repositories;
using TicketFlow.API.Services;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.Tests.Services;

// Shared base class for all TicketService tests.
// Contains helpers used across test files:
// CreateDb, BuildService, SeedUserAndCategoryAsync
public abstract class TicketServiceTestBase
{
    
    //Creates a fresh in-memory EF Core database.
    // Each test gets its own database (unique name) so tests don't bleed into each other.
   
    protected static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // Builds a TicketService with:
    // - Real repository + real in-memory DB (to test actual EF Core logic)
    // - Mocked cache (avoid Redis dependency)
    // - Mocked SignalR hub (avoid real-time connection dependency)
    // - Mocked logger (logs don't affect test results)

    protected static (TicketService service, AppDbContext db) BuildService(string dbName)
    {
        var db = CreateDb(dbName);

        // Hangfire needs a storage backend even in tests — use in-memory
        GlobalConfiguration.Configuration.UseInMemoryStorage();

        // Real repository so we test actual DB logic (repository + EF Core)
        var repo = new TicketRepository(db);

        // Mock TicketActivity repository — we don't assert on activity logging here
        var mockActivityRepo = new Mock<ITicketActivityRepository>();

        // Mock cache — always return null (cache miss) so every call hits the DB
        var mockCache = new Mock<ICacheService>();
        mockCache.Setup(c => c.GetAsync<TicketDto>(It.IsAny<string>())).ReturnsAsync((TicketDto?)null);
        mockCache.Setup(c => c.GetAsync<IEnumerable<TicketDto>>(It.IsAny<string>())).ReturnsAsync((IEnumerable<TicketDto>?)null);
        mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<TicketDto>(), It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);
        mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<TicketDto>>(), It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);
        mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Mock SignalR hub — we don't want to test real-time connections here
        var mockHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Mock logger — log output doesn't affect test results
        var mockLogger = new Mock<ILogger<TicketService>>();


        //new
        var service = new TicketService(repo, mockActivityRepo.Object, db, mockCache.Object, mockLogger.Object, mockHub.Object);
        return (service, db);
    }

    // Seeds a User and Category into the database.

    protected static async Task<(User user, Category category)> SeedUserAndCategoryAsync(AppDbContext db)
    {
        var user = new User { FullName = "Test User", Email = "test@example.com" };
        var category = new Category { Name = "Bug" };
        db.Users.Add(user);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return (user, category);
    }
}