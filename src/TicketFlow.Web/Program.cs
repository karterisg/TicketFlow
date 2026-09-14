using MudBlazor.Services;
using TicketFlow.Web.Components;
using TicketFlow.Web.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https://localhost:7216/");
});

builder.Services.AddScoped<ApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new ApiService(factory.CreateClient("api"));
});


builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NotificationState>();




builder.Services.AddSignalR();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/status/{0}", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
//forgeries
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
