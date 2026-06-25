using AspNetCoreRateLimit;
using DotNetEnv;
using Legi.Identity.Api.Middleware;
using Legi.Identity.Application;
using Legi.Identity.Infrastructure;
using Legi.Identity.Infrastructure.Persistence;
using Legi.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

// Load environment variables from .env file
// Search in current directory and parent directories (solution root)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envPath))
{
    // Try solution root (two levels up from project directory)
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

// Docker secrets (production): each file under /run/secrets becomes a config key,
// e.g. /run/secrets/Jwt__PrivateKey -> Jwt:PrivateKey. Added last so it takes
// precedence over environment variables, keeping secrets out of `docker inspect`
// and /proc/<pid>/environ. optional:true -> a no-op in dev (the dir is absent).
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Forwarded headers — the API sits behind nginx and a host TLS proxy. Honor
// X-Forwarded-For/Proto so rate limiting, login lockout and Turnstile key off the
// real client IP (not the proxy) and HTTPS is detected correctly. Trust only the
// private proxy networks to prevent clients from spoofing their IP.
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

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.PostConfigure<IpRateLimitOptions>(options =>
{
    options.RealIpHeader = null;
    options.ClientIdHeader = null;
});
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are required.");
jwtSettings.Validate();

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

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Legi Identity API",
        Version = "v1",
        Description = "Legi API for user authentication and management"
    });

    // JWT no Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insert the token JWT with the format: Bearer {seu_token}"
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
    scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
    if (args.Contains("--migrate"))
        return;
}

// Middleware pipeline

// Must run first so every downstream component sees the real client IP/scheme.
app.UseForwardedHeaders();

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

// Rate limiting (before authentication)
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
