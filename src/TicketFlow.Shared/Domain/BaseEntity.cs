using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//shared models for the domain layer

namespace TicketFlow.Shared.Domain
{
    public abstract class BaseEntity
    {
        public int  Id { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
