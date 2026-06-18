using Legi.Catalog.Application.Books.Commands.SetBookCover;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Commands.SetBookCover;

public class SetBookCoverCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IWorkRepository> _workRepository = new();
    private readonly SetBookCoverCommandHandler _handler;

    private const string CoverUrl = "/covers/9780132350884/owned.webp";

    public SetBookCoverCommandHandlerTests()
    {
        _handler = new SetBookCoverCommandHandler(
            _bookRepository.Object,
            _workRepository.Object,
            NullLogger<SetBookCoverCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PersistsCoverUrl_AndBackfillsWork_WhenBookIsCoverLess()
    {
        var book = DomainBookFactory.Create();
        var work = Work.Create(WorkKey.Synthesize("t", "a"), "t");
        _bookRepository.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _workRepository.Setup(r => r.GetByIdAsync(book.WorkId, It.IsAny<CancellationToken>())).ReturnsAsync(work);

        var result = await _handler.Handle(
            new SetBookCoverCommand(book.Id, Guid.NewGuid(), CoverUrl), CancellationToken.None);

        Assert.Equal(CoverUrl, result.CoverUrl);
        Assert.Equal(CoverUrl, book.CoverUrl);
        // The work had no cover → it gets backfilled with the same URL.
        Assert.Equal(CoverUrl, work.DefaultCoverUrl);
        _bookRepository.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenBookAlreadyHasCover()
    {
        var book = DomainBookFactory.Create();
        book.UpdateDetails(coverUrl: "https://existing.example/cover.jpg");
        _bookRepository.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);

        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(
            new SetBookCoverCommand(book.Id, Guid.NewGuid(), CoverUrl), CancellationToken.None));

        _bookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_WhenBookMissing()
    {
        _bookRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new SetBookCoverCommand(Guid.NewGuid(), Guid.NewGuid(), CoverUrl), CancellationToken.None));
    }
}
