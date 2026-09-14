using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Enrichers;
using TicketFlow.API.Data;
using TicketFlow.API.Hubs;
using TicketFlow.API.Interfaces;
using TicketFlow.API.Jobs;
using TicketFlow.API.Middleware;
using TicketFlow.API.Repositories;
using TicketFlow.API.Services;
using TicketFlow.API.Settings;
using TicketFlow.API.Validators;
using TicketFlow.Shared.Domain;
using TicketFlow.API.Authorization;
using System.Text;



var builder = WebApplication.CreateBuilder(args);
 

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/ticketflow-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();



builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


builder.Services.AddFluentValidationAutoValidation();
//builder.Services.AddValidatorsFromAssemblyContaining<CreateTicketValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAgentValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
builder.Services.AddEndpointsApiExplorer();


//builder.Services.AddSwaggerGen();

//to authorize the jwt token in swagger ui
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "TicketFlow API", Version = "v1" });

    // add JWT for swagger
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});





//for SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


//for Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();


//for JWT Authentication //Options pattern
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// var secretKey = jwtSettings["SecretKey"]!; //
//beacause of options pattern we changed to :
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()!;

var secretKey = jwtSettings.SecretKey;



//for JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey))
    };
});

//for Authorization
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, TicketFlow.API.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, TicketFlow.API.Authorization.PermissionAuthorizationHandler>();
builder.Services.AddScoped<TokenService>();

// DI
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketActivityRepository, TicketActivityRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectAccessService, ProjectAccessService>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ITeamAccessService, TeamAccessService>();
builder.Services.AddScoped<IMeetingRepository, MeetingRepository>();
builder.Services.AddScoped<IMeetingService, MeetingService>();


builder.Services.AddDistributedMemoryCache();

// Redis Cache (for prod) — uncomment when Redis
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
// });

builder.Services.AddScoped<ICacheService, CacheService>();

// Resend email
builder.Services.Configure<ResendSettings>(builder.Configuration.GetSection("Resend"));
builder.Services.AddHttpClient<IEmailService, ResendEmailService>();

//----------for hangfire to do processes in the background with having to send http  requests-----------------

//app.UseHangfireServer(); //then:
builder.Services.AddHangfire(config =>
{
    if (builder.Environment.IsDevelopment())
        config.UseInMemoryStorage();
    else
        config.UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHangfireServer();


// CORS : used to allow using api
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7216", "http://localhost:5217")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSignalR();

var app = builder.Build();


app.UseCors("BlazorPolicy");

//app.UseHangfireDashboard("/hangfire");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireManagerAuthFilter() }
});


// Recurring Jobs //=>what job we have,and updates the job after requeue
RecurringJob.AddOrUpdate<TicketJobs>(
    "check-overdue-tickets",
    job => job.CheckOverdueTicketsAsync(),
    "0 * * * *"); // all time

RecurringJob.AddOrUpdate<TicketJobs>(
    "daily-report",
    job => job.GenerateDailyReportAsync(),
    "0 8 * * *"); // every day at 8

RecurringJob.AddOrUpdate<TicketFlow.API.Jobs.NotificationJobs>(
    "cleanup-read-notifications",
    job => job.CleanupReadNotificationsAsync(),
    "0 3 * * *"); // every day at 3am
//------------------------------------


// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();  // Added this line to enable authentication
app.UseAuthorization(); // Added this line to enable authorization
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Ensure DB and Identity roles exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Customer", "Agent", "Manager" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    await TicketFlow.API.Authorization.PermissionCatalog.SeedAsync(db);
}

app.Run();