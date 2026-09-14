using Microsoft.JSInterop;
using TicketFlow.Shared.DTOs;
using System.Text.Json;

namespace TicketFlow.Web.Services;

public class AuthService
{
    private const string StorageKey = "sd_auth";
    private readonly ApiService _apiService;
    private AuthResponseDto? _currentUser;

    public AuthService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public bool IsAuthenticated => _currentUser is not null;
    public string? Role => _currentUser?.Role;
    public string? FullName => _currentUser?.FullName;
    public string? Email => _currentUser?.Email;
    public int? UserId => _currentUser?.UserId;
    public bool IsManager => Role == "Manager";
    public bool IsAgent => Role == "Agent";
    public bool IsCustomer => Role == "Customer";



    public string? AppUserId => _currentUser?.AppUserId; //keeps track of the app user id for the current logged in user//the user in the row needed
    public int? AgentId => _currentUser?.AgentId;

    private HashSet<string> _permissions = new();

    public bool HasPermission(string key) => IsManager || _permissions.Contains(key);

    public async Task RestoreAsync(IJSRuntime js)
    {
        if (_currentUser is not null) return;
        try
        {
            var json = await js.InvokeAsync<string?>("authStorage.load", StorageKey);
            if (string.IsNullOrEmpty(json)) return;
            var dto = JsonSerializer.Deserialize<AuthResponseDto>(json);
            if (dto is null || dto.ExpiresAt <= DateTime.UtcNow) return;
            _currentUser = dto;
            _apiService.SetToken(dto.Token);
            await LoadPermissionsAsync();
        }
        catch { }
    }

    public async Task<bool> LoginAsync(string email, string password, IJSRuntime js)
    {
        var result = await _apiService.LoginAsync(new LoginDto { Email = email, Password = password });
        if (result is null) return false;
        _currentUser = result;
        _apiService.SetToken(result.Token);
        await LoadPermissionsAsync();
        await PersistAsync(js, result);
        return true;
    }

    private async Task LoadPermissionsAsync()
    {
        if (IsManager) { _permissions = new(); return; }
        var keys = await _apiService.GetMyPermissionsAsync();
        _permissions = new HashSet<string>(keys);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(string fullName, string email, string password, string role, IJSRuntime js)
    {
        var (result, error) = await _apiService.RegisterAsync(new RegisterDto
        {
            FullName = fullName,
            Email = email,
            Password = password,
            Role = role
        });
        if (result is null) return (false, error);
        _currentUser = result;
        _apiService.SetToken(result.Token);
        await PersistAsync(js, result);
        return (true, null);
    }

    public async Task LogoutAsync(IJSRuntime js)
    {
        _currentUser = null;
        _permissions = new();
        _apiService.SetToken(string.Empty);
        try { await js.InvokeVoidAsync("authStorage.remove", StorageKey); } catch { }
    }

    private static async Task PersistAsync(IJSRuntime js, AuthResponseDto dto)
    {
        try
        {
            var json = JsonSerializer.Serialize(dto);
            await js.InvokeVoidAsync("authStorage.save", StorageKey, json);
        }
        catch { }
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (_currentUser is null) return (false, "Not authenticated.");
        return await _apiService.ChangePasswordAsync(new ChangePasswordDto
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        });
    }


}
