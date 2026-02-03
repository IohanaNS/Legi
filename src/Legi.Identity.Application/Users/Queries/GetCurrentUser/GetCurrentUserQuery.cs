using MediatR;

namespace Legi.Identity.Application.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<GetCurrentUserResponse>;