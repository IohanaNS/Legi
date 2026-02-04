using Legi.Identity.Application.Common.Mediator;

namespace Legi.Identity.Application.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<GetCurrentUserResponse>;