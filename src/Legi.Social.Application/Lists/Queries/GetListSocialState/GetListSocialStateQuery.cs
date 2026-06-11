using Legi.Social.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Queries.GetListSocialState;

public record GetListSocialStateQuery(
    Guid ListId,
    Guid? ViewerUserId) : IRequest<ListSocialStateDto>;
