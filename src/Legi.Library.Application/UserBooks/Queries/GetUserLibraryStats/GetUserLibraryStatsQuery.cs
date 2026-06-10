using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetUserLibraryStats;

public record GetUserLibraryStatsQuery(
    Guid TargetUserId,
    Guid ViewerUserId
) : IRequest<UserLibraryStatsDto>;
