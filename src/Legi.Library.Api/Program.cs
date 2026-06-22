using DotNetEnv;
using Legi.Library.Api.Middleware;
using Legi.Library.Application;
using Legi.Library.Infrastructure;
using Legi.Library.Infrastructure.Persistence;
using Legi.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.HttpOverrides;

// Load environment variables from .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envPath))
{
    envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
}
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    Console.WriteLine("Warning: .env file not found. Make sure environment variables are set.");
}

var builder = WebApplication.CreateBuilder(args);

// Application & Infrastructure layers
builder.Services.AddLibraryApplication();
builder.Services.AddLibraryInfrastructure(builder.Configuration);

// Forwarded headers — the API sits behind nginx and a host TLS proxy. Honor
// X-Forwarded-For/Proto so HTTPS is detected correctly (no redirect loops) and any
// client-IP logic sees the real client. Trust only the private proxy networks to
// prevent clients from spoofing their IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = builder.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 2;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    var trustedNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>()
        ?? ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];
    foreach (var cidr in trustedNetworks)
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
    }
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

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
        IssuerSigningKey = jwtSettings.CreatePublicSigningKey(),
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Legi Library API",
        Version = "v1",
        Description = "API for managing user libraries, reading posts, and book lists"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insert the JWT token with the format: Bearer {your_token}"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Schema migration (Fase 6 6D.1). Single-instance dev migrates on startup by
// default; for multi-replica, run the image with `--migrate` as a one-shot step
// and set RunMigrationsOnStartup=false so replicas don't race.
if (args.Contains("--migrate") || builder.Configuration.GetValue("RunMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.Migrate();
    if (args.Contains("--migrate"))
        return;
}

// Must run first so every downstream component sees the real client IP/scheme.
app.UseForwardedHeaders();

// Exception handling (first in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
