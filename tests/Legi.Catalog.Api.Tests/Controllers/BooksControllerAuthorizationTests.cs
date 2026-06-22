using System.Security.Claims;
using Legi.Catalog.Api.Authorization;
using Legi.Catalog.Api.Controllers;
using Legi.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Catalog.Api.Tests.Controllers;

public class BooksControllerAuthorizationTests
{
    [Theory]
    [InlineData(nameof(BooksController.UpdateBook))]
    [InlineData(nameof(BooksController.DeleteBook))]
    [InlineData(nameof(BooksController.UploadCover))]
    public void CatalogManagementEndpoints_RequireManageCatalogBooksPolicy(string actionName)
    {
        var attribute = GetAuthorizeAttribute(actionName);

        Assert.Equal(LegiAuthPolicies.CanManageCatalogBooks, attribute.Policy);
    }

    [Fact]
    public void CreateBook_AllowsAnyAuthenticatedUser()
    {
        var attribute = GetAuthorizeAttribute(nameof(BooksController.CreateBook));

        Assert.Null(attribute.Policy);
    }

    [Fact]
    public async Task CanManageCatalogBooksPolicy_DeniesRegularUser()
    {
        using var serviceProvider = CreateAuthorizationServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var user = UserWithRole(LegiAuthRoles.User);

        var result = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            LegiAuthPolicies.CanManageCatalogBooks);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CanManageCatalogBooksPolicy_AllowsAdmin()
    {
        using var serviceProvider = CreateAuthorizationServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var user = UserWithRole(LegiAuthRoles.Admin);

        var result = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            LegiAuthPolicies.CanManageCatalogBooks);

        Assert.True(result.Succeeded);
    }

    private static AuthorizeAttribute GetAuthorizeAttribute(string actionName)
    {
        var method = typeof(BooksController).GetMethod(actionName)
            ?? throw new InvalidOperationException($"Action {actionName} was not found.");

        return method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .OfType<AuthorizeAttribute>()
            .Single();
    }

    private static ServiceProvider CreateAuthorizationServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddCatalogAuthorizationPolicies());

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal UserWithRole(string role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}
