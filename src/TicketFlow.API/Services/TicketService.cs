using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketFlow.API.Data;
using TicketFlow.API.Exceptions;
using TicketFlow.API.Hubs;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Jobs;
using TicketFlow.API.Repositories;
using TicketFlow.API.Mapping;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;
using System.Net.Sockets;


namespace TicketFlow.API.Services;

public class TicketService(
    ITicketRepository ticketRepository,
    ITicketActivityRepository activityRepository,
    AppDbContext context,
    ICacheService cache,
    ILogger<TicketService> logger,
    INotificationService notificationService) : ITicketService

{
    private const string AllTicketsCacheKey = "tickets:all";

    public async Task<IEnumerable<TicketDto>> GetAllTicketsAsync(int? forUserId = null, int? forAgentId = null) //tickets going to each user(customer) and agent 
    {
        logger.LogInformation("Fetching all tickets");

        if (forUserId.HasValue)
        {
            var tickets = await ticketRepository.GetAllAsync();
            return tickets.Where(t => t.UserId == forUserId.Value).ToDtoList(); //to the specified users
        }

        if (forAgentId.HasValue)
        {
            var tickets = await ticketRepository.GetAllAsync();
            return tickets.Where(t => t.AgentId == forAgentId.Value).ToDtoList();
        }

        var cached = await cache.GetAsync<IEnumerable<TicketDto>>(AllTicketsCacheKey);
        if (cached is not null)
        {
            logger.LogInformation("Returning tickets from cache");
            return cached;
        }

        var allTickets = await ticketRepository.GetAllAsync();
        var dtos = allTickets.ToDtoList();
        await cache.SetAsync(AllTicketsCacheKey, dtos, TimeSpan.FromMinutes(5));

        logger.LogInformation("Returning {Count} tickets from database", dtos.Count());
        return dtos;
    }

    //not use of cache cause many filters and search, so caching would be complex and not efficient
    public async Task<PagedResultDto<TicketDto>> GetPagedTicketsAsync(
        int page, int pageSize, string? search, List<string>? statuses, int? forUserId = null, int? forAgentId = null)
    {
        logger.LogInformation(
            "Fetching tickets page {Page} (size {PageSize}, search='{Search}', statuses={Statuses}) for user={UserId} agent={AgentId}",
            page, pageSize, search, statuses is null ? "none" : string.Join(',', statuses), forUserId, forAgentId);

        var (items, totalCount) = await ticketRepository.GetPagedAsync(page, pageSize, search, statuses, forUserId, forAgentId);

        logger.LogInformation("Returned {Count}/{Total} tickets for page {Page}", items.Count, totalCount, page);

        return new PagedResultDto<TicketDto>
        {
            Items = items.ToDtoList().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TicketStatsDto> GetTicketStatsAsync(int? forAgentId = null)
    {
        logger.LogInformation("Fetching ticket stats");

        var query = context.Tickets.AsQueryable();
        if (forAgentId.HasValue)
            query = query.Where(t => t.AgentId == forAgentId.Value);

        var byStatus = await query
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var byPriority = await query
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();



        return new TicketStatsDto
        {
            Total = byStatus.Sum(s => s.Count),
            ByStatus = byStatus.ToDictionary(s => s.Status, s => s.Count),
            ByPriority = byPriority.ToDictionary(p => p.Priority, p => p.Count)
        };
    }

    public async Task<TicketDto?> GetTicketByIdAsync(int id)
    {
        logger.LogInformation("Fetching ticket {TicketId}", id);

        var cacheKey = $"tickets:{id}";
        var cached = await cache.GetAsync<TicketDto>(cacheKey);
        if (cached is not null)
        {
            logger.LogInformation("Returning ticket {TicketId} from cache", id);
            return cached;
        }

        var ticket = await ticketRepository.GetByIdAsync(id);
        if (ticket is null)
        {
            logger.LogWarning("Ticket {TicketId} not found", id);
            return null;
        }

        var dto = ticket.ToDto();
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<Ticket> CreateTicketAsync(
        string title, string description, int userId, int categoryId, string priority = "Medium", DateTime? dueDate = null, int? projectId = null)
    {
        logger.LogInformation("Creating ticket for user {UserId}", userId);

        var userExists = await context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            logger.LogWarning("User {UserId} not found", userId);
            throw new NotFoundException("User", userId);
        }

        var categoryExists = await context.Categories.AnyAsync(c => c.Id == categoryId);
        if (!categoryExists)
        {
            logger.LogWarning("Category {CategoryId} not found", categoryId);
            throw new NotFoundException("Category", categoryId);
        }

        if (projectId.HasValue)
        {
            var project = await context.Projects.FindAsync(projectId.Value);
            if (project is null)
            {
                logger.LogWarning("Project {ProjectId} not found", projectId.Value);
                throw new NotFoundException("Project", projectId.Value);
            }

            if (!project.AllowCustomerTicketCreation)
            {
                logger.LogWarning("Project {ProjectId} does not allow customer ticket creation", projectId.Value);
                throw new ForbiddenException("This project does not accept new tickets from customers.");
            }
        }

        var ticket = new Ticket(title, description, userId, categoryId, dueDate) { ProjectId = projectId };//paremetric constructor for ticket creation, setting the projectId if provided


        if (Enum.TryParse<TicketPriority>(priority, true, out var parsedPriority))
            ticket.Priority = parsedPriority;
        var created = await ticketRepository.CreateAsync(ticket);


        await activityRepository.AddAsync(new TicketActivity(
            created.Id, TicketActivityType.Created,
            $"Ticket #{created.TicketNumber} created.", userId, "System"));

        // Fire and forget for hangfire
        BackgroundJob.Enqueue<TicketJobs>(
            job => job.NotifyAgentsAsync(created.Id));

        BackgroundJob.Enqueue<TicketJobs>(
            job => job.SendTicketCreatedEmailAsync(created.Id));

        if (projectId.HasValue)
            BackgroundJob.Enqueue<TicketJobs>(
                job => job.SendProjectNotificationEmailAsync(created.Id));

        logger.LogInformation("Ticket {TicketId} created successfully", created.Id);
        await cache.RemoveAsync(AllTicketsCacheKey);

        // CreateTicketAsync notification
        await notificationService.NotifyAsync(userId,
            $"Ticket #{created.TicketNumber} '{created.Title}' created successfully.", "success");


        return created;
    }

    public async Task AssignTicketAsync(int ticketId, int agentId)
    {
        logger.LogInformation("Assigning ticket {TicketId} to agent {AgentId}", ticketId, agentId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId)
            ?? throw new NotFoundException("Ticket", ticketId);

        var agent = await context.Agents.FindAsync(agentId)
            ?? throw new NotFoundException("Agent", agentId);

        if (!agent.IsAvailable)
        {
            logger.LogWarning("Agent {AgentId} is not available", agentId);
            throw new BadRequestException($"Agent {agent.FullName} is not available.");
        }

        ticket.Assign(agent);
        await ticketRepository.UpdateAsync(ticket);



        await activityRepository.AddAsync(new TicketActivity(
            ticketId, TicketActivityType.Assigned,
            $"Ticket assigned to agent {agent.FullName}.", agentId, agent.FullName));


        logger.LogInformation("Ticket {TicketId} assigned to agent {AgentId}", ticketId, agentId);
        await cache.RemoveAsync(AllTicketsCacheKey);
        await cache.RemoveAsync($"tickets:{ticketId}");

        //sends notification to agent
        // AssignTicketAsync notification
        await notificationService.NotifyAsync(ticket.UserId,
            $"Ticket #{ticket.TicketNumber} '{ticket.Title}' has been assigned to {agent.FullName}.", "info");
    }

    public async Task SetDueDateAsync(int ticketId, DateTime? dueDate)
    {
        logger.LogInformation("Setting due date for ticket {TicketId}", ticketId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId)
            ?? throw new NotFoundException("Ticket", ticketId);

        ticket.DueDate = dueDate;
        await ticketRepository.UpdateAsync(ticket);

        await activityRepository.AddAsync(new TicketActivity(
            ticketId, TicketActivityType.DueDateChanged,
            dueDate.HasValue ? $"Deadline set to {dueDate.Value:g}." : "Deadline removed.",
            ticket.UserId, "System"));

        logger.LogInformation("Ticket {TicketId} due date set", ticketId);
        await cache.RemoveAsync(AllTicketsCacheKey);
        await cache.RemoveAsync($"tickets:{ticketId}");
    }

    public async Task ResolveTicketAsync(int ticketId)
    {
        logger.LogInformation("Resolving ticket {TicketId}", ticketId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId)
            ?? throw new NotFoundException("Ticket", ticketId);

        ticket.Resolve();
        await ticketRepository.UpdateAsync(ticket);


        await activityRepository.AddAsync(new TicketActivity(
            ticketId, TicketActivityType.Resolved,
            $"Ticket resolved.", ticket.UserId, "System"));


        logger.LogInformation("Ticket {TicketId} resolved", ticketId);
        await cache.RemoveAsync(AllTicketsCacheKey);
        await cache.RemoveAsync($"tickets:{ticketId}");
    }

    public async Task CloseTicketAsync(int ticketId, bool isManager)
    {
        logger.LogInformation("Closing ticket {TicketId}", ticketId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId)
            ?? throw new NotFoundException("Ticket", ticketId);

        if (ticket.ProjectId.HasValue)
        {
            var project = await context.Projects.FindAsync(ticket.ProjectId.Value);
            if (project?.RequireApprovalForClose == true && !isManager)
            {
                logger.LogWarning("Ticket {TicketId} requires manager approval to close", ticketId);
                throw new ForbiddenException("Manager approval is required to close tickets in this project.");
            }
        }

        ticket.Close();
        await ticketRepository.UpdateAsync(ticket);


        await activityRepository.AddAsync(new TicketActivity(
            ticketId, TicketActivityType.Closed,
            $"Ticket closed.", ticket.UserId, "System"));


        logger.LogInformation("Ticket {TicketId} closed", ticketId);
        await cache.RemoveAsync(AllTicketsCacheKey);
        await cache.RemoveAsync($"tickets:{ticketId}");
    }

    public async Task DeleteTicketAsync(int ticketId)
    {
        logger.LogInformation("Deleting ticket {TicketId}", ticketId);

        var exists = await context.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!exists)
            throw new NotFoundException("Ticket", ticketId);

        await ticketRepository.DeleteAsync(ticketId);

        await activityRepository.AddAsync(new TicketActivity(
            ticketId, TicketActivityType.Deleted,
            $"Ticket deleted.", null, "System"));

        logger.LogInformation("Ticket {TicketId} soft deleted", ticketId);
        await cache.RemoveAsync(AllTicketsCacheKey);
        await cache.RemoveAsync($"tickets:{ticketId}");
    }
}