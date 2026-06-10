using FluentValidation;
using Legi.Library.Application;
using Legi.Library.Application.Common.Behaviors;
using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Application.Common.Policies;
using Legi.Library.Application.ReadingPosts.Commands.CreateReadingPost;
using Legi.Library.Application.ReadingPosts.EventHandlers;
using Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;
using Legi.Library.Application.UserBooks.Commands.UpdateUserBook;
using Legi.Library.Domain.Events;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Library.Application.Tests.DependencyInjection;

public class DependencyInjectionTests
{
    [Fact]
    public void AddLibraryApplication_ShouldRegisterMediatorBehaviorsValidatorsAndHandlers()
    {
        var services = new ServiceCollection();

        var result = services.AddLibraryApplication();

        Assert.Same(services, result);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IMediator) &&
            d.ImplementationType == typeof(Mediator) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IUserListVisibilityPolicy) &&
            d.ImplementationType == typeof(UserListVisibilityPolicy) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(LoggingBehavior<,>) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ValidationBehavior<,>) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<AddBookToLibraryCommand, AddBookToLibraryResponse>) &&
            d.ImplementationType == typeof(AddBookToLibraryCommandHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<CreateReadingPostCommand, CreateReadingPostResponse>) &&
            d.ImplementationType == typeof(CreateReadingPostCommandHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(INotificationHandler<ReadingProgressCreatedDomainEvent>) &&
            d.ImplementationType == typeof(ReadingProgressCreatedDomainEventHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IValidator<UpdateUserBookCommand>) &&
            d.ImplementationType == typeof(UpdateUserBookCommandValidator) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddLibraryApplication_ShouldRegisterNotificationHandlerForDomainEvent()
    {
        var services = new ServiceCollection();

        services.AddLibraryApplication();

        var handlerCount = services.Count(d =>
            d.ServiceType == typeof(INotificationHandler<ReadingProgressCreatedDomainEvent>));

        Assert.Equal(1, handlerCount);
    }
}
