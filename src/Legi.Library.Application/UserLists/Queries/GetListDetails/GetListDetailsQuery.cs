using Legi.Library.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Queries.GetListDetails;

public record GetListDetailsQuery(
    Guid ListId
) : IRequest<UserListDetailDto>;
