using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Extensions;
using TicketFlow.Shared.Domain;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets/{ticketId}/comments")]
public class CommentsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(int ticketId)
    {
        var comments = await context.Comments
            .Where(c => c.TicketId == ticketId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Body = c.Body,
                CreatedAt = c.CreatedAt,
                AuthorName = c.Author.FullName
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int ticketId, [FromBody] CreateCommentDto dto)
    {
        var ticketExists = await context.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists) return NotFound();

        var authorId = User.GetUserId();
        if (authorId is null) return Forbid();

        var comment = new Comment
        {
            Body = dto.Body,
            TicketId = ticketId,
            AuthorId = authorId.Value
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        await context.Entry(comment).Reference(c => c.Author).LoadAsync();

        return Ok(new CommentDto
        {
            Id = comment.Id,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            AuthorName = comment.Author.FullName
        });
    }
}
