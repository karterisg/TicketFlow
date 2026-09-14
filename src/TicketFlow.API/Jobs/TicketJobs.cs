using Hangfire;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using TicketFlow.Shared.Domain;
using System.Net.Sockets;

//for hangfire
namespace TicketFlow.API.Jobs;


public class TicketJobs(AppDbContext context, ILogger<TicketJobs> logger, IEmailService emailService)
{
    public async Task SendTicketCreatedEmailAsync(int ticketId)
    {
        var ticket = await context.Tickets
            .Include(t => t.User)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null || ticket.User is null)
        {
            logger.LogWarning("Cannot send ticket-created email: ticket {TicketId} or its user was not found", ticketId);
            return;
        }

        var dueDateRow = ticket.DueDate.HasValue
            ? $"""
               <tr>
                 <td style="padding:10px 28px 0;color:#8aa0c0;font-size:13px;">Deadline: <span style="color:#e8f6ff;font-weight:600;">{ticket.DueDate.Value:MMM d, yyyy HH:mm}</span></td>
               </tr>
               """
            : "";

        var subject = $"Ticket #{ticket.TicketNumber} created: {ticket.Title}";
        var html = $"""
            <div style="background:#05040a;padding:32px 16px;font-family:'Segoe UI',Arial,sans-serif;">
              <table role="presentation" width="100%" style="max-width:480px;margin:0 auto;background:#0b0a16;border:1px solid rgba(0,240,255,0.25);border-radius:8px;overflow:hidden;">
                <tr>
                  <td style="height:3px;background:linear-gradient(90deg,#00f0ff,#ff2ec4);font-size:0;line-height:0;">&nbsp;</td>
                </tr>
                <tr>
                  <td style="padding:28px 28px 8px;">
                    <div style="font-size:12px;letter-spacing:3px;color:#00f0ff;text-transform:uppercase;font-weight:700;">Support&nbsp;<span style="color:#ff2ec4;">Desk</span></div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:8px 28px 0;">
                    <div style="display:inline-block;font-family:monospace;font-size:12px;color:#39ff8f;border:1px solid rgba(57,255,143,0.4);border-radius:20px;padding:3px 10px;">TICKET CREATED</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 28px 0;">
                    <div style="font-size:19px;font-weight:700;color:#e8f6ff;">#{ticket.TicketNumber} &middot; {ticket.Title}</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:14px 28px 0;color:#8aa0c0;font-size:13px;line-height:1.4;">Hi {ticket.User.FullName},</td>
                </tr>
                <tr>
                  <td style="padding:6px 28px 0;color:#8aa0c0;font-size:13px;line-height:1.4;">Your ticket has been created successfully. Our team will pick it up shortly.</td>
                </tr>
                <tr>
                  <td style="padding:14px 28px 0;color:#8aa0c0;font-size:13px;">Priority: <span style="color:#e8f6ff;font-weight:600;">{ticket.Priority}</span></td>
                </tr>
                <tr>
                  <td style="padding:6px 28px 0;color:#8aa0c0;font-size:13px;">Category: <span style="color:#e8f6ff;font-weight:600;">{ticket.Category.Name}</span></td>
                </tr>
                {dueDateRow}
                <tr>
                  <td style="padding:18px 28px 0;">
                    <div style="background:#12101f;border:1px solid rgba(0,240,255,0.14);border-radius:4px;padding:14px 16px;color:#e8f6ff;font-size:13px;line-height:1.5;">
                      {ticket.Description}
                    </div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:24px 28px 28px;color:#4a5578;font-size:11px;">We'll notify you here when there's an update on this ticket.</td>
                </tr>
              </table>
            </div>
            """;

        await emailService.SendEmailAsync(ticket.User.Email, subject, html);
    }

    public async Task SendProjectNotificationEmailAsync(int ticketId)
    {
        var ticket = await context.Tickets
            .Include(t => t.Project)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket?.Project?.NotificationEmail is null)
            return;

        var dueDateRow = ticket.DueDate.HasValue
            ? $"""
               <tr>
                 <td style="padding:10px 28px 0;color:#8aa0c0;font-size:13px;">Deadline: <span style="color:#e8f6ff;font-weight:600;">{ticket.DueDate.Value:MMM d, yyyy HH:mm}</span></td>
               </tr>
               """
            : "";

        var subject = $"New ticket in {ticket.Project.Name}: #{ticket.TicketNumber} {ticket.Title}";
        var html = $"""
            <div style="background:#05040a;padding:32px 16px;font-family:'Segoe UI',Arial,sans-serif;">
              <table role="presentation" width="100%" style="max-width:480px;margin:0 auto;background:#0b0a16;border:1px solid rgba(0,240,255,0.25);border-radius:8px;overflow:hidden;">
                <tr>
                  <td style="height:3px;background:linear-gradient(90deg,#00f0ff,#ff2ec4);font-size:0;line-height:0;">&nbsp;</td>
                </tr>
                <tr>
                  <td style="padding:28px 28px 8px;">
                    <div style="font-size:12px;letter-spacing:3px;color:#00f0ff;text-transform:uppercase;font-weight:700;">Support&nbsp;<span style="color:#ff2ec4;">Desk</span></div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:8px 28px 0;">
                    <div style="display:inline-block;font-family:monospace;font-size:12px;color:#39ff8f;border:1px solid rgba(57,255,143,0.4);border-radius:20px;padding:3px 10px;">NEW TICKET</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 28px 0;">
                    <div style="font-size:19px;font-weight:700;color:#e8f6ff;">#{ticket.TicketNumber} &middot; {ticket.Title}</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:14px 28px 0;color:#8aa0c0;font-size:13px;line-height:1.4;">A new ticket was created in project "{ticket.Project.Name}".</td>
                </tr>
                <tr>
                  <td style="padding:14px 28px 0;color:#8aa0c0;font-size:13px;">Priority: <span style="color:#e8f6ff;font-weight:600;">{ticket.Priority}</span></td>
                </tr>
                <tr>
                  <td style="padding:6px 28px 0;color:#8aa0c0;font-size:13px;">Category: <span style="color:#e8f6ff;font-weight:600;">{ticket.Category.Name}</span></td>
                </tr>
                {dueDateRow}
                <tr>
                  <td style="padding:18px 28px 0;">
                    <div style="background:#12101f;border:1px solid rgba(0,240,255,0.14);border-radius:4px;padding:14px 16px;color:#e8f6ff;font-size:13px;line-height:1.5;">
                      {ticket.Description}
                    </div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:24px 28px 28px;color:#4a5578;font-size:11px;">This is an automated project notification.</td>
                </tr>
              </table>
            </div>
            """;

        await emailService.SendEmailAsync(ticket.Project.NotificationEmail, subject, html);
    }

    public async Task CheckOverdueTicketsAsync()
    {
        logger.LogInformation("Checking for overdue tickets");
        var overdueTickets = await context.Tickets
        .Include(t => t.User)
        .Include(t => t.Agent)
        .Where(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
            && t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow)
        .ToListAsync();

        foreach (var ticket in overdueTickets)
        {
            logger.LogWarning("Ticket {TicketId} '{Title}' passed its deadline ({DueDate}) — closing it.",
            ticket.Id,
            ticket.Title,
            ticket.DueDate);
            ticket.Close();
        }

        if (overdueTickets.Count > 0)
            await context.SaveChangesAsync();

        logger.LogInformation(
       "Closed {Count} overdue tickets", overdueTickets.Count);
    }

    public async Task GenerateDailyReportAsync()
    {
        logger.LogInformation("Generating daily report");

        var today = DateTime.UtcNow.Date;

        var openTickets = await context.Tickets.CountAsync(t => t.Status == TicketStatus.Open);

        var resolvedToday = await context.Tickets.CountAsync(t => t.Status == TicketStatus.Resolved
        && t.ResolvedAt >= today);

        var createdToday = await context.Tickets.CountAsync(t => t.CreatedAt >= today);

        logger.LogInformation("Daily Report — Open: {Open}, Resolved Today: {Resolved}, Created Today: {Created}",
            openTickets,
            resolvedToday,
            createdToday);
    }

    public async Task NotifyAgentsAsync(int ticketId)
    {
        logger.LogInformation("Notifying agents about new ticket {TicketId}", ticketId);

        var ticket = await context.Tickets
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null) return;

        var availableAgents = await context.Agents
            .Where(a => a.IsAvailable)
            .ToListAsync();

        logger.LogInformation(
            "New ticket '{Title}' in category '{Category}' — {AgentCount} agents available",
            ticket.Title,
            ticket.Category.Name,
            availableAgents.Count);
    }
}
