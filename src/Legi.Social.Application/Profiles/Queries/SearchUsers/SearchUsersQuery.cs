using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Profiles.Queries.SearchUsers;

public record SearchUsersQuery(
    string UsernamePrefix,
    Guid? ViewerUserId,
    int Limit = 10) : IRequest<IReadOnlyList<FollowUserDto>>;
