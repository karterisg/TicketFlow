namespace TicketFlow.Shared.DTOs
{
    public class MeetingDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public string? LocationOrLink { get; set; }
        public string Status { get; set; } = string.Empty;
        public int AttendeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MeetingAttendeeDto> Attendees { get; set; } = new();
    }

    public class CreateMeetingDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ProjectId { get; set; }
        public int? TeamId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public string? LocationOrLink { get; set; }
        public List<MeetingAttendeeInputDto> Attendees { get; set; } = new();
    }

    public class MeetingAttendeeInputDto
    {
        public int? UserId { get; set; }
        public int? AgentId { get; set; }
    }

    public class MeetingAttendeeDto
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public int? UserId { get; set; }
        public int? AgentId { get; set; }
        public string AttendeeName { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
    }

    public class RespondToMeetingDto
    {
        public int? UserId { get; set; }
        public int? AgentId { get; set; }
        public string Response { get; set; } = "Accepted";
    }
}
