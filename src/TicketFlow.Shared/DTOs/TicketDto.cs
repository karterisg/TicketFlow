using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketFlow.Shared.DTOs
{
    public class TicketDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }





        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public int? AgentId { get; set; }

        public string? AgentName { get; set; }
        public int  CommentCount { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int TicketNumber { get; set; } //its the ticket number (because some them have been soft deleted)

        public DateTime? DueDate { get; set; }

        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
    }

    public class CreateTicketDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public string Priority { get; set; } = "Medium";

        public DateTime? DueDate { get; set; }

        public int? ProjectId { get; set; }
    }

    public class AssignTicketDto
    {
        public int AgentId { get; set; }
    }

    public class SetDueDateDto
    {
        public DateTime? DueDate { get; set; }
    }
}
