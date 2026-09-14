using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketFlow.Shared.Domain
{
    public abstract class Person : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        
        public virtual string GetDisplayName()=> FullName;
    }
}
