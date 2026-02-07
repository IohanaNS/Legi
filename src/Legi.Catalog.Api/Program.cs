using Legi.Catalog.Api.Middleware;
using Legi.Catalog.Application;
using Legi.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ===== Add Services =====

// Application & Infrastructure layers
builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Legi Catalog API",
        Version = "v1",
        Description = "API for managing the global book catalog and user libraries"
    });
});

// ===== Build App =====

var app = builder.Build();

// ===== Configure Middleware Pipeline =====

// Exception handling (first in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Legi Catalog API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();

// TODO: Add authentication when integrated with Identity service
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();