using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Infrastructure.ExternalServices;
using Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;
using Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;

namespace Legi.Catalog.Infrastructure;

/// <summary>
/// Extension methods for registering external book data services.
/// Call this from your existing DependencyInjection.cs or directly from Program.cs.
/// </summary>
public static class ExternalServicesRegistration
{
    public static IServiceCollection AddExternalBookServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Settings ---
        var openLibrarySettings = new OpenLibrarySettings();
        configuration.GetSection(OpenLibrarySettings.SectionName).Bind(openLibrarySettings);

        var googleBooksSettings = new GoogleBooksSettings();
        configuration.GetSection(GoogleBooksSettings.SectionName).Bind(googleBooksSettings);

        services.Configure<GoogleBooksSettings>(
            configuration.GetSection(GoogleBooksSettings.SectionName));

        services.Configure<OpenLibrarySettings>(
            configuration.GetSection(OpenLibrarySettings.SectionName));

        // --- Open Library (Priority 1: free, no API key) ---
        if (openLibrarySettings.Enabled)
        {
            services.AddHttpClient<OpenLibraryClient>(client =>
            {
                client.BaseAddress = new Uri("https://openlibrary.org");
                client.Timeout = TimeSpan.FromSeconds(openLibrarySettings.TimeoutSeconds);
                // Open Library asks for a User-Agent to identify your app
                client.DefaultRequestHeaders.Add("User-Agent", "Legi/1.0 (book-catalog-app)");
            });

            services.AddScoped<IExternalBookClient, OpenLibraryClient>();
        }

        // --- Google Books (Priority 2: fallback, optional API key) ---
        if (googleBooksSettings.Enabled)
        {
            services.AddHttpClient<GoogleBooksClient>(client =>
            {
                client.BaseAddress = new Uri("https://www.googleapis.com");
                client.Timeout = TimeSpan.FromSeconds(googleBooksSettings.TimeoutSeconds);
            });

            services.AddScoped<IExternalBookClient, GoogleBooksClient>();
        }

        // --- Orchestrator ---
        services.AddScoped<IBookDataProvider, BookDataProvider>();

        return services;
    }
}