using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketFlow.Shared.Domain
{
    public class User : Person
    {
        public List<Ticket> Tickets { get; set; } = new();

        public override string GetDisplayName() => $"{FullName} ({Email})";
    }
}
