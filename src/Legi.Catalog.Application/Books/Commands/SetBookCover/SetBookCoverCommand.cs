using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Books.Commands.SetBookCover;

/// <summary>
/// Manually attach a cover to a <strong>cover-less</strong> book — the definitive
/// escape hatch for the long tail no provider has a cover for. The controller has
/// already validated, processed and stored the image to the owned bucket; this
/// command persists the resulting blob URL. Fill-only: it will not overwrite an
/// existing cover. Carries only the URL (no image bytes) so it's safe to log.
/// </summary>
public sealed record SetBookCoverCommand(
    Guid BookId,
    Guid UserId,
    string CoverUrl) : IRequest<SetBookCoverResponse>;
