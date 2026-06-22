using Legi.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Legi.Catalog.Api.Authorization;

public static class CatalogAuthorizationOptionsExtensions
{
    public static void AddCatalogAuthorizationPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(LegiAuthPolicies.CanManageCatalogBooks, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(LegiAuthRoles.Admin);
        });
    }
}
