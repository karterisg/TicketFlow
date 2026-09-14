using Microsoft.EntityFrameworkCore;
using TicketFlow.API.Data;
using TicketFlow.API.Interfaces;
using System.Threading;
using TicketFlow.Shared.Domain;
using System.Threading;


//using Repositories to use the AppDbContext to access the database and perform CRUD operations on the Ticket entity
namespace TicketFlow.API.Repositories
{
    public class TicketRepository(AppDbContext context) : ITicketRepository
    {
        public async Task<IEnumerable<Ticket>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.Tickets
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Agent)
                .Include(t => t.Category)
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<Ticket> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, List<string>? statuses,
            int? forUserId, int? forAgentId, CancellationToken cancellationToken = default)
        {
            var query = context.Tickets
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Agent)
                .Include(t => t.Category)
                .Include(t => t.Project)
                .AsQueryable();

            if (forUserId.HasValue)
                query = query.Where(t => t.UserId == forUserId.Value);

            if (forAgentId.HasValue)
                query = query.Where(t => t.AgentId == forAgentId.Value);

            if (statuses is { Count: > 0 })
                query = query.Where(t => statuses.Contains(t.Status.ToString()));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t =>
                    t.Title.Contains(search) ||
                    t.User.FullName.Contains(search) ||
                    t.Category.Name.Contains(search));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        //public async Task<IEnumerable<Ticket>> GetAllAsync()


        public async Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await context.Tickets
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Agent)
                .Include(t => t.Category)
                .Include(t => t.Project)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
        public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken = default)
        {
            ticket.TicketNumber = context.Database
                .SqlQuery<int>($"SELECT nextval('\"TicketNumberSeq\"')::int")
                .Single();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync(cancellationToken);
            return ticket;
        }

        public async Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            context.Entry(ticket).State = EntityState.Modified; //marks only the ticket itself as modified, avoiding conflicts with related entities (User/Agent/Category/Project) already tracked elsewhere in the same request
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var ticket = await context.Tickets.FindAsync(new object[] { id }, cancellationToken); //finds the ticket by id
            if (ticket is not null)
            {
                ticket.Delete();
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}