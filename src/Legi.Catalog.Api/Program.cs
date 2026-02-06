using Legi.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();