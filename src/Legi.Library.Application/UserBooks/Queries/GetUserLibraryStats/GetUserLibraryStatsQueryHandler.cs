using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Domain.Enums;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetUserLibraryStats;

public class GetUserLibraryStatsQueryHandler(
    IUserBookReadRepository userBookReadRepository,
    IUserListReadRepository userListReadRepository)
    : IRequestHandler<GetUserLibraryStatsQuery, UserLibraryStatsDto>
{
    public async Task<UserLibraryStatsDto> Handle(
        GetUserLibraryStatsQuery request,
        CancellationToken cancellationToken)
    {
        var counts = await userBookReadRepository.GetStatusCountsByUserIdAsync(
            request.TargetUserId,
            cancellationToken);

        var lists = await userListReadRepository.CountVisibleByUserIdAsync(
            request.TargetUserId,
            request.ViewerUserId,
            cancellationToken);

        return new UserLibraryStatsDto(
            Reading: GetCount(counts, ReadingStatus.Reading),
            Finished: GetCount(counts, ReadingStatus.Finished),
            Paused: GetCount(counts, ReadingStatus.Paused),
            Abandoned: GetCount(counts, ReadingStatus.Abandoned),
            NotStarted: GetCount(counts, ReadingStatus.NotStarted),
            Lists: lists);
    }

    private static int GetCount(
        IReadOnlyDictionary<ReadingStatus, int> counts,
        ReadingStatus status) =>
        counts.TryGetValue(status, out var count) ? count : 0;
}
