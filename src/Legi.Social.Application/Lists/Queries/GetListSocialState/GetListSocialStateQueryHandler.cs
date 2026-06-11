using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Queries.GetListSocialState;

public class GetListSocialStateQueryHandler(IListSocialReadRepository readRepository)
    : IRequestHandler<GetListSocialStateQuery, ListSocialStateDto>
{
    public Task<ListSocialStateDto> Handle(
        GetListSocialStateQuery request,
        CancellationToken cancellationToken)
    {
        return readRepository.GetStateAsync(
            request.ListId, request.ViewerUserId, cancellationToken);
    }
}
