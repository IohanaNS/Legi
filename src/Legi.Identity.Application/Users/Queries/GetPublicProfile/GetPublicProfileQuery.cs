using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Queries.GetPublicProfile;

public record GetPublicProfileQuery(
    Guid UserId,
    Guid? CurrentUserId = null // Null se não autenticado
) : IRequest<GetPublicProfileResponse>;