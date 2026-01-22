using System.Text;
using FestGuide.Api.Hubs;
using FestGuide.Application;
using FestGuide.Application.Services;
using FestGuide.DataAccess;
using FestGuide.Infrastructure;
using FestGuide.Integrations;
using FestGuide.Integrations.PushNotifications;
using FestGuide.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add JWT configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration not found.");

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    // Set maximum message size to 128KB to support large schedule updates
    options.MaximumReceiveMessageSize = 128 * 1024;

    // Enable detailed errors in development for easier debugging
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors = true;
    }

    // Configure keep-alive and timeout intervals for varying network conditions
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add authentication
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
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add custom services
// IMPORTANT: Service registration order matters for IEmailService.
// AddInfrastructureServices registers ConsoleEmailService (fallback for development).
// AddIntegrationServices registers SmtpEmailService (production email via SMTP).
// The last registration wins, so AddIntegrationServices must be called after
// AddInfrastructureServices to ensure SMTP is used when configured.
var baseUrl = builder.Configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
builder.Services.AddInfrastructureServices(baseUrl);
builder.Services.AddSecurityServices();
builder.Services.AddApplicationServices();
builder.Services.AddDataAccessServices(connectionString);
builder.Services.AddIntegrationServices(builder.Configuration);

// Phase 5 - Push notification provider (stub for development)
builder.Services.AddScoped<IPushNotificationProvider, StubPushNotificationProvider>();

// Phase 5 - SignalR hub service
builder.Services.AddScoped<IScheduleHubService, ScheduleHubService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<ScheduleHub>("/hubs/schedule");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();
