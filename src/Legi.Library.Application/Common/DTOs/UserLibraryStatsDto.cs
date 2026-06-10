namespace Legi.Library.Application.Common.DTOs;

public record UserLibraryStatsDto(
    int Reading,
    int Finished,
    int Paused,
    int Abandoned,
    int NotStarted,
    int Lists
);
