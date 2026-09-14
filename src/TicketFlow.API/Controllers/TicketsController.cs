using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.API.Extensions;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Mapping;
using TicketFlow.API.Reporting;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TicketsController(ITicketService ticketService, Microsoft.AspNetCore.Authorization.IAuthorizationService authorizationService) : ControllerBase
{
    private async Task<bool> CanViewAllAsync()
    {
        if (User.IsInRole("Manager")) return true;
        var result = await authorizationService.AuthorizeAsync(User, "Perm:Tickets.ViewAll");
        return result.Succeeded;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var canViewAll = await CanViewAllAsync();
        var isManager = User.IsInRole("Manager");
        var isCustomer = !isManager && User.IsInRole("Customer") && !canViewAll;
        var isAgent = !isManager && !isCustomer && User.IsInRole("Agent");

        var tickets = await ticketService.GetAllTicketsAsync(
            isCustomer ? User.GetUserId() : null,
            isAgent ? User.GetAgentId() : null);

        return Ok(tickets); //return 200ok
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var canViewAll = await CanViewAllAsync();
        var isManager = User.IsInRole("Manager");
        var isCustomer = !isManager && User.IsInRole("Customer") && !canViewAll;
        var isAgent = !isManager && !isCustomer && User.IsInRole("Agent");

        var statuses = string.IsNullOrWhiteSpace(status)
            ? null
            : status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var result = await ticketService.GetPagedTicketsAsync(
            page, pageSize, search, statuses,
            isCustomer ? User.GetUserId() : null,
            isAgent ? User.GetAgentId() : null);

        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var isManager = User.IsInRole("Manager");
        var isCustomer = !isManager && User.IsInRole("Customer");
        var isAgent = !isManager && !isCustomer && User.IsInRole("Agent");

        if (isCustomer)
        {
            var canViewReports = (await authorizationService.AuthorizeAsync(User, "Perm:Reports.View")).Succeeded;
            if (!canViewReports)
                return Forbid();
        }

        var stats = await ticketService.GetTicketStatsAsync(isAgent ? User.GetAgentId() : null);
        return Ok(stats);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var tickets = await ticketService.GetAllTicketsAsync();
        var bytes = TicketExcelExporter.Export(tickets);
        var fileName = $"tickets-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await ticketService.GetTicketByIdAsync(id);
        if (ticket is null)
            return NotFound();

        var isManager = User.IsInRole("Manager");
        var isCustomer = !isManager && User.IsInRole("Customer");
        var isAgent = !isManager && !isCustomer && User.IsInRole("Agent");

        if (isCustomer && ticket.UserId != User.GetUserId())
            return NotFound();

        if (isAgent && ticket.AgentId != User.GetAgentId())
            return NotFound();

        return Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketDto dto)
    {
        var isManager = User.IsInRole("Manager");
        var userId = isManager ? dto.UserId : (User.GetUserId() ?? dto.UserId);

        var ticket = await ticketService.CreateTicketAsync(
            dto.Title,
            dto.Description,
            userId,
            dto.CategoryId,
            dto.Priority,
            dto.DueDate,
            dto.ProjectId);

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket.ToDto());
    }

    [Authorize(Policy = "Perm:Tickets.Assign")]
    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignTicketDto dto)
    {
        await ticketService.AssignTicketAsync(id, dto.AgentId);
        return NoContent();
    }

    [Authorize(Roles = "Manager,Agent")]
    [HttpPatch("{id}/due-date")]
    public async Task<IActionResult> SetDueDate(int id, [FromBody] SetDueDateDto dto)
    {
        await ticketService.SetDueDateAsync(id, dto.DueDate);
        return NoContent();
    }



    [Authorize(Roles = "Manager,Agent")]
    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id)
    {
        await ticketService.ResolveTicketAsync(id);
        return NoContent();
    }

    [Authorize(Policy = "Perm:Tickets.Close")]
    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(int id)
    {
        await ticketService.CloseTicketAsync(id, User.IsInRole("Manager"));
        return NoContent();
    }

    [Authorize(Policy = "Perm:Tickets.Delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await ticketService.DeleteTicketAsync(id);
        return NoContent();
    }
}