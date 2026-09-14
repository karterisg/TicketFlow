using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Settings;

namespace TicketFlow.API.Services;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(HttpClient httpClient, IOptions<ResendSettings> settings, ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            from = $"{_settings.FromName} <{_settings.FromEmail}>",
            to = new[] { to },
            subject,
            html = htmlBody
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("emails", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Resend email failed ({StatusCode}) to {To}: {Body}", response.StatusCode, to, body);
            return;
        }

        _logger.LogInformation("Email sent to {To} via Resend", to);
    }
}


