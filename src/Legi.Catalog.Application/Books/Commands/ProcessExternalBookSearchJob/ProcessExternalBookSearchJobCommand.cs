using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Books.Commands.ProcessExternalBookSearchJob;

public record ProcessExternalBookSearchJobCommand(
    string SearchTerm,
    Guid RequestedByUserId,
    int MaxResults
) : IRequest<ProcessExternalBookSearchJobResponse>;
