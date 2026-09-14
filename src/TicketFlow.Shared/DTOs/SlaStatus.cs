using System;

namespace TicketFlow.Shared.DTOs
{
    public class SlaStatus
    {
        public bool IsBreached { get; set; }

        public bool IsWarning { get; set; }

        public int MinutesRemaining { get; set; }

        public string Color { get; set; } = "gray";

        // Warning kicks in once less than this many minutes remain before the deadline.
        private const int WarningThresholdMinutes = 60;
        public bool IsOpen { get; set; }

        public static SlaStatus Calculate(DateTime? dueDate, DateTime? resolvedAt, string status)
        {
            if (!dueDate.HasValue)
                return new SlaStatus { Color = "gray" };

            var isClosed = status is "Resolved" or "Closed";
            var reference = isClosed && resolvedAt.HasValue ? resolvedAt.Value : DateTime.UtcNow;
            var minutesRemaining = (int)(dueDate.Value - reference).TotalMinutes;

            var isBreached = minutesRemaining < 0;
            var isWarning = !isBreached && !isClosed && minutesRemaining <= WarningThresholdMinutes;
            var isOpen = !isClosed && !isBreached && !isWarning;

            return new SlaStatus
            {
                IsBreached = isBreached,
                IsWarning = isWarning,
                MinutesRemaining = minutesRemaining,
                IsOpen = isOpen,
                Color = isBreached ? "red" : isWarning ? "orange" : isOpen ? "blue" : "green"
            };
        }
    }
}
