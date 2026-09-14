using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.Web.Services;


//ui controllers to see what backend handles
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }


    private async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return default;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }

    private async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        var content = data is null
            ? new StringContent("{}", Encoding.UTF8, "application/json")
            : new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private async Task<bool> PostEmptyAsync(string endpoint, object? data = null)
    {
        var content = data is null
            ? new StringContent("{}", Encoding.UTF8, "application/json")
            : new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> PatchAsync(string endpoint, object data)
    {
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content };
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // Tickets
    public Task<List<TicketDto>> GetTicketsAsync(int? actingUserId = null, int? actingAgentId = null)
    {
        var query = new List<string>();
        if (actingUserId.HasValue) query.Add($"actingUserId={actingUserId}");
        if (actingAgentId.HasValue) query.Add($"actingAgentId={actingAgentId}");
        var endpoint = query.Count > 0 ? $"api/tickets?{string.Join('&', query)}" : "api/tickets";

        return GetAsync<List<TicketDto>>(endpoint).ContinueWith(t => t.IsCompletedSuccessfully ? t.Result ?? [] : []);
    }

    public Task<PagedResultDto<TicketDto>?> GetTicketsPagedAsync(
        int page, int pageSize, string? search, List<string>? statuses,
        int? actingUserId = null, int? actingAgentId = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (statuses is { Count: > 0 }) query.Add($"status={Uri.EscapeDataString(string.Join(',', statuses))}");
        if (actingUserId.HasValue) query.Add($"actingUserId={actingUserId}");
        if (actingAgentId.HasValue) query.Add($"actingAgentId={actingAgentId}");

        var endpoint = $"api/tickets/paged?{string.Join('&', query)}";
        return GetAsync<PagedResultDto<TicketDto>>(endpoint);
    }

    public Task<TicketDto?> GetTicketAsync(int id, int? actingUserId = null, int? actingAgentId = null)
    {
        var query = new List<string>();
        if (actingUserId.HasValue) query.Add($"actingUserId={actingUserId}");
        if (actingAgentId.HasValue) query.Add($"actingAgentId={actingAgentId}");
        var endpoint = query.Count > 0 ? $"api/tickets/{id}?{string.Join('&', query)}" : $"api/tickets/{id}";

        return GetAsync<TicketDto>(endpoint);
    }

    public Task<TicketDto?> CreateTicketAsync(CreateTicketDto dto)
        => PostAsync<TicketDto>("api/tickets", dto);

    public Task<bool> AssignTicketAsync(int id, int agentId)
        => PostEmptyAsync($"api/tickets/{id}/assign", new { AgentId = agentId });

    public Task<bool> SetDueDateAsync(int id, DateTime? dueDate)
        => PatchAsync($"api/tickets/{id}/due-date", new { DueDate = dueDate });

    public Task<bool> ResolveTicketAsync(int id)
        => PostEmptyAsync($"api/tickets/{id}/resolve");

    public Task<bool> CloseTicketAsync(int id)
        => PostEmptyAsync($"api/tickets/{id}/close");

    public Task<bool> DeleteTicketAsync(int id)
        => DeleteAsync($"api/tickets/{id}");

    public async Task<byte[]?> ExportTicketsToExcelAsync()
    {
        var response = await _httpClient.GetAsync("api/tickets/export");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    // Reports
    public Task<TicketStatsDto?> GetTicketStatsAsync(int? actingAgentId = null)
    {
        var endpoint = actingAgentId.HasValue ? $"api/tickets/stats?actingAgentId={actingAgentId}" : "api/tickets/stats";
        return GetAsync<TicketStatsDto>(endpoint);
    }



    // Comments
    public Task<List<CommentDto>> GetCommentsAsync(int ticketId)
        => GetAsync<List<CommentDto>>($"api/tickets/{ticketId}/comments").ContinueWith(t => t.Result ?? []);

    public Task<CommentDto?> AddCommentAsync(int ticketId, CreateCommentDto dto)
        => PostAsync<CommentDto>($"api/tickets/{ticketId}/comments", dto);


    public Task<List<TicketActivityDto>> GetTicketActivitiesAsync(int ticketId)
        => GetAsync<List<TicketActivityDto>>($"api/tickets/{ticketId}/activities").ContinueWith(t => t.Result ?? []);

    // Rating
    public Task<RatingDto?> GetRatingAsync(int ticketId)
        => GetAsync<RatingDto>($"api/tickets/{ticketId}/rating");

    public Task<RatingDto?> SubmitRatingAsync(int ticketId, SubmitRatingDto dto)
        => PostAsync<RatingDto>($"api/tickets/{ticketId}/rating", dto);

    // Notifications
    public Task<List<NotificationDto>> GetNotificationsAsync()
        => GetAsync<List<NotificationDto>>("api/notifications").ContinueWith(t => t.Result ?? []);

    public Task<int> GetUnreadNotificationCountAsync()
        => GetAsync<int>("api/notifications/unread-count");

    public Task<bool> MarkNotificationsReadAsync()
        => PostEmptyAsync("api/notifications/mark-read");



    // Agents
    public Task<List<AgentDto>> GetAgentsAsync()
        => GetAsync<List<AgentDto>>("api/agents").ContinueWith(t => t.Result ?? []);

    public Task<PagedResultDto<AgentDto>?> GetAgentsPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        return GetAsync<PagedResultDto<AgentDto>>($"api/agents/paged?{string.Join('&', query)}");
    }

    public Task<AgentDto?> CreateAgentAsync(CreateAgentDto dto)
        => PostAsync<AgentDto>("api/agents", dto);

    public Task<bool> SetAgentAvailabilityAsync(int id, bool isAvailable)
        => PatchAsync($"api/agents/{id}/availability", new { IsAvailable = isAvailable });

    // Categories
    public Task<List<CategoryDto>> GetCategoriesAsync()
        => GetAsync<List<CategoryDto>>("api/categories").ContinueWith(t => t.Result ?? []);

    public Task<PagedResultDto<CategoryDto>?> GetCategoriesPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        return GetAsync<PagedResultDto<CategoryDto>>($"api/categories/paged?{string.Join('&', query)}");
    }

    public Task<bool> DeleteCategoryAsync(int id)
        => DeleteAsync($"api/categories/{id}");

    public Task<CategoryDto?> CreateCategoryAsync(string name)
        => PostAsync<CategoryDto>("api/categories", new { Name = name });

    public Task<bool> UpdateCategoryAsync(int id, string name)
    {
        var content = new StringContent(JsonSerializer.Serialize(new UpdateCategoryDto { Name = name }), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/categories/{id}") { Content = content };
        return _httpClient.SendAsync(request).ContinueWith(t => t.IsCompletedSuccessfully && t.Result.IsSuccessStatusCode);
    }





    // Users
    public Task<List<UserDto>> GetUsersAsync()
        => GetAsync<List<UserDto>>("api/users").ContinueWith(t => t.Result ?? []);

    public Task<PagedResultDto<UserDto>?> GetUsersPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        return GetAsync<PagedResultDto<UserDto>>($"api/users/paged?{string.Join('&', query)}");
    }

    public Task<UserDto?> CreateUserAsync(string fullName, string email, string password)
        => PostAsync<UserDto>("api/users", new { FullName = fullName, Email = email, Password = password });

    public Task<AgentDto?> PromoteUserToAgentAsync(int userId)
        => PostAsync<AgentDto>($"api/users/{userId}/promote-to-agent");

    public Task<bool> DeleteUsersAsync(int id)
    => DeleteAsync($"api/users/{id}");

    // Auth
    public Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        => PostAsync<AuthResponseDto>("api/auth/login", dto);

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(ChangePasswordDto dto)
    {
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/auth/change-password", content);
        if (response.IsSuccessStatusCode) return (true, null);

        var body = await response.Content.ReadAsStringAsync();
        return (false, ParseApiError(body));
    }

    public async Task<(AuthResponseDto? Data, string? Error)> RegisterAsync(RegisterDto dto)
    {
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/auth/register", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (null, ParseApiError(body));

        return (JsonSerializer.Deserialize<AuthResponseDto>(body, _jsonOptions), null);
    }

    private static string ParseApiError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // ASP.NET Identity returns an array: [{code, description}, ...]
            if (root.ValueKind == JsonValueKind.Array)
            {
                var messages = root.EnumerateArray()
                    .Select(e => e.TryGetProperty("description", out var d) ? d.GetString() : null)
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(" ", messages);
                if (!string.IsNullOrWhiteSpace(joined)) return joined;
            }

            // Conflict / bad request: {message: "..."}
            if (root.TryGetProperty("message", out var msg))
                return msg.GetString() ?? "Registration failed.";
        }
        catch { }
        return "Registration failed.";
    }

 
    public Task<List<TimeEntryDto>> GetTimeEntriesAsync(int ticketId)
        => GetAsync<List<TimeEntryDto>>($"api/tickets/{ticketId}/time-entries")
           .ContinueWith(t => t.Result ?? []);

    public Task<bool> StartTimerAsync(int ticketId, int agentId)
        => PostEmptyAsync($"api/tickets/{ticketId}/time-entries/start", new StartTimerDto { AgentId = agentId });

    public Task<bool> StopTimerAsync(int ticketId, int agentId)
        => PostEmptyAsync($"api/tickets/{ticketId}/time-entries/stop", new StopTimerDto { AgentId = agentId });

    public Task<bool> LogTimeAsync(int ticketId, int agentId, int minutes, string? note)
        => PostEmptyAsync($"api/tickets/{ticketId}/time-entries/log", new LogTimeDto { AgentId = agentId, Minutes = minutes, Note = note });

    // Projects
    public Task<List<ProjectDto>> GetProjectsAsync(int? forUserId = null, int? forAgentId = null)
    {
        var query = new List<string>();
        if (forUserId.HasValue) query.Add($"forUserId={forUserId}");
        if (forAgentId.HasValue) query.Add($"forAgentId={forAgentId}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return GetAsync<List<ProjectDto>>($"api/projects{qs}").ContinueWith(t => t.Result ?? []);
    }

    public Task<PagedResultDto<ProjectDto>?> GetProjectsPagedAsync(int page, int pageSize, string? search = null, int? forUserId = null, int? forAgentId = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (forUserId.HasValue) query.Add($"forUserId={forUserId}");
        if (forAgentId.HasValue) query.Add($"forAgentId={forAgentId}");
        return GetAsync<PagedResultDto<ProjectDto>>($"api/projects/paged?{string.Join('&', query)}");
    }

    public Task<ProjectDto?> GetProjectAsync(int id)
        => GetAsync<ProjectDto>($"api/projects/{id}");

    public Task<ProjectDto?> CreateProjectAsync(CreateProjectDto dto)
        => PostAsync<ProjectDto>("api/projects", dto);

    public Task<ProjectDto?> CreateProjectFromTemplateAsync(CreateProjectFromTemplateDto dto)
        => PostAsync<ProjectDto>("api/projects/from-template", dto);

    public Task<bool> ArchiveProjectAsync(int id, int actingUserId)
        => PostEmptyAsync($"api/projects/{id}/archive?actingUserId={actingUserId}");

    public Task<bool> DeleteProjectAsync(int id, int actingUserId)
        => DeleteAsync($"api/projects/{id}?actingUserId={actingUserId}");

    public Task<bool> UpdateProjectSettingsAsync(int id, int actingUserId, UpdateProjectSettingsDto dto)
    {
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        return _httpClient.PutAsync($"api/projects/{id}/settings?actingUserId={actingUserId}", content)
            .ContinueWith(t => t.IsCompletedSuccessfully && t.Result.IsSuccessStatusCode);
    }

    // Project categories
    public Task<List<ProjectCategoryDto>> GetProjectCategoriesAsync()
        => GetAsync<List<ProjectCategoryDto>>("api/projectcategories").ContinueWith(t => t.Result ?? []);

    public Task<PagedResultDto<ProjectCategoryDto>?> GetProjectCategoriesPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        return GetAsync<PagedResultDto<ProjectCategoryDto>>($"api/projectcategories/paged?{string.Join('&', query)}");
    }

    public Task<ProjectCategoryDto?> CreateProjectCategoryAsync(string name)
        => PostAsync<ProjectCategoryDto>("api/projectcategories", new CreateProjectCategoryDto { Name = name });

    public Task<bool> DeleteProjectCategoryAsync(int id)
        => DeleteAsync($"api/projectcategories/{id}");

    public Task<bool> UpdateProjectCategoryAsync(int id, string name)
    {
        var content = new StringContent(JsonSerializer.Serialize(new UpdateProjectCategoryDto { Name = name }), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/projectcategories/{id}") { Content = content };
        return _httpClient.SendAsync(request).ContinueWith(t => t.IsCompletedSuccessfully && t.Result.IsSuccessStatusCode);
    }

    // Project templates
    public Task<List<ProjectTemplateDto>> GetProjectTemplatesAsync()
        => GetAsync<List<ProjectTemplateDto>>("api/projecttemplates").ContinueWith(t => t.Result ?? []);

    public Task<PagedResultDto<ProjectTemplateDto>?> GetProjectTemplatesPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        return GetAsync<PagedResultDto<ProjectTemplateDto>>($"api/projecttemplates/paged?{string.Join('&', query)}");
    }

    public Task<ProjectTemplateDto?> CreateProjectTemplateAsync(CreateProjectTemplateDto dto)
        => PostAsync<ProjectTemplateDto>("api/projecttemplates", dto);

    public Task<bool> UpdateProjectTemplateAsync(int id, UpdateProjectTemplateDto dto)
    {
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/projecttemplates/{id}") { Content = content };
        return _httpClient.SendAsync(request).ContinueWith(t => t.IsCompletedSuccessfully && t.Result.IsSuccessStatusCode);
    }

    public Task<bool> DeleteProjectTemplateAsync(int id)
        => DeleteAsync($"api/projecttemplates/{id}");

    // Project members
    public Task<List<ProjectMemberDto>> GetProjectMembersAsync(int projectId)
        => GetAsync<List<ProjectMemberDto>>($"api/projects/{projectId}/members").ContinueWith(t => t.Result ?? []);

    public Task<bool> AddProjectMemberAsync(int projectId, int actingUserId, AddProjectMemberDto dto)
        => PostEmptyAsync($"api/projects/{projectId}/members?actingUserId={actingUserId}", dto);

    public Task<bool> ChangeProjectMemberRoleAsync(int projectId, int memberId, int actingUserId, string role)
    {
        var content = new StringContent(JsonSerializer.Serialize(new ChangeProjectMemberRoleDto { Role = role }), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/projects/{projectId}/members/{memberId}/role?actingUserId={actingUserId}") { Content = content };
        return _httpClient.SendAsync(request).ContinueWith(t => t.IsCompletedSuccessfully && t.Result.IsSuccessStatusCode);
    }

    public Task<bool> RemoveProjectMemberAsync(int projectId, int memberId, int actingUserId)
        => DeleteAsync($"api/projects/{projectId}/members/{memberId}?actingUserId={actingUserId}");

    // Teams
    public Task<List<TeamDto>> GetTeamsAsync(int? forUserId = null, int? forAgentId = null)
    {
        var query = new List<string>();
        if (forUserId.HasValue) query.Add($"forUserId={forUserId}");
        if (forAgentId.HasValue) query.Add($"forAgentId={forAgentId}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return GetAsync<List<TeamDto>>($"api/teams{qs}").ContinueWith(t => t.Result ?? []);
    }

    public Task<PagedResultDto<TeamDto>?> GetTeamsPagedAsync(int page, int pageSize, string? search = null, int? forUserId = null, int? forAgentId = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (forUserId.HasValue) query.Add($"forUserId={forUserId}");
        if (forAgentId.HasValue) query.Add($"forAgentId={forAgentId}");
        return GetAsync<PagedResultDto<TeamDto>>($"api/teams/paged?{string.Join('&', query)}");
    }

    public Task<TeamDto?> GetTeamAsync(int id)
        => GetAsync<TeamDto>($"api/teams/{id}");

    public Task<TeamDto?> CreateTeamAsync(CreateTeamDto dto)
        => PostAsync<TeamDto>("api/teams", dto);

    public Task<bool> DeleteTeamAsync(int id, int actingUserId)
        => DeleteAsync($"api/teams/{id}?actingUserId={actingUserId}");

    public Task<List<ProjectTeamDto>> GetTeamProjectsAsync(int teamId)
        => GetAsync<List<ProjectTeamDto>>($"api/teams/{teamId}/projects").ContinueWith(t => t.Result ?? []);

    public Task<bool> AssignTeamToProjectAsync(int teamId, int projectId, int actingUserId)
        => PostEmptyAsync($"api/teams/{teamId}/projects/{projectId}?actingUserId={actingUserId}");

    public Task<bool> UnassignTeamFromProjectAsync(int teamId, int projectId, int actingUserId)
        => DeleteAsync($"api/teams/{teamId}/projects/{projectId}?actingUserId={actingUserId}");

 



    // Team members
    public Task<List<TeamMemberDto>> GetTeamMembersAsync(int teamId)
        => GetAsync<List<TeamMemberDto>>($"api/teams/{teamId}/members").ContinueWith(t => t.Result ?? []);

    public Task<bool> AddTeamMemberAsync(int teamId, int actingUserId, AddTeamMemberDto dto)
        => PostEmptyAsync($"api/teams/{teamId}/members?actingUserId={actingUserId}", dto);

    public Task<bool> ChangeTeamMemberRoleAsync(int teamId, int memberId, int actingUserId, string role)
    {
        var content = new StringContent(JsonSerializer.Serialize(new ChangeTeamMemberRoleDto { Role = role }), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Put, $"api/teams/{teamId}/members/{memberId}/role?actingUserId={actingUserId}") { Content = content };
        return _httpClient.SendAsync(request).ContinueWith(t => t.IsCompletedSuccessfully && t.Result.IsSuccessStatusCode);
    }

    public Task<bool> RemoveTeamMemberAsync(int teamId, int memberId, int actingUserId)
        => DeleteAsync($"api/teams/{teamId}/members/{memberId}?actingUserId={actingUserId}");




    // Meetings
    public Task<List<MeetingDto>> GetMeetingsAsync(int? projectId = null, int? teamId = null)
    {
        var query = new List<string>();
        if (projectId.HasValue) query.Add($"projectId={projectId}");
        if (teamId.HasValue) query.Add($"teamId={teamId}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return GetAsync<List<MeetingDto>>($"api/meetings{qs}").ContinueWith(t => t.Result ?? []);
    }

    public Task<PagedResultDto<MeetingDto>?> GetMeetingsPagedAsync(int page, int pageSize, string? search = null, int? forUserId = null, int? forAgentId = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (forUserId.HasValue) query.Add($"forUserId={forUserId}");
        if (forAgentId.HasValue) query.Add($"forAgentId={forAgentId}");
        return GetAsync<PagedResultDto<MeetingDto>>($"api/meetings/paged?{string.Join('&', query)}");
    }

    public Task<MeetingDto?> GetMeetingAsync(int id)
        => GetAsync<MeetingDto>($"api/meetings/{id}");

    public Task<MeetingDto?> CreateMeetingAsync(CreateMeetingDto dto)
        => PostAsync<MeetingDto>("api/meetings", dto);

    public Task<bool> CancelMeetingAsync(int id, int actingUserId)
        => PostEmptyAsync($"api/meetings/{id}/cancel?actingUserId={actingUserId}");

    public Task<bool> RespondToMeetingAsync(int id, RespondToMeetingDto dto)
        => PostEmptyAsync($"api/meetings/{id}/respond", dto);

    // Permissions
    public Task<List<PermissionDto>> GetAllPermissionsAsync()
        => GetAsync<List<PermissionDto>>("api/permissions").ContinueWith(t => t.IsCompletedSuccessfully ? t.Result ?? [] : []);

    public Task<List<string>> GetMyPermissionsAsync()
        => GetAsync<List<string>>("api/permissions/me").ContinueWith(t => t.IsCompletedSuccessfully ? t.Result ?? [] : []);

    public Task<List<string>> GetUserPermissionsAsync(string applicationUserId)
        => GetAsync<List<string>>($"api/permissions/user/{applicationUserId}").ContinueWith(t => t.IsCompletedSuccessfully ? t.Result ?? [] : []);

    public async Task<bool> UpdateUserPermissionsAsync(string applicationUserId, List<string> grantedKeys)
    {
        var content = new StringContent(JsonSerializer.Serialize(new UpdateUserPermissionsDto { GrantedKeys = grantedKeys }), Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"api/permissions/user/{applicationUserId}", content);
        return response.IsSuccessStatusCode;
    }
}
