using FluentValidation;
using Legi.Catalog.Application;
using Legi.Catalog.Application.Books.Commands.CreateBook;
using Legi.Catalog.Application.Books.EventHandlers;
using Legi.Catalog.Application.Common.Behaviors;
using Legi.Catalog.Domain.Events;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Catalog.Application.Tests.DependencyInjection;

public class DependencyInjectionTests
{
    [Fact]
    public void AddCatalogApplication_ShouldRegisterMediatorBehaviorsValidatorsAndHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddCatalogApplication();

        // Assert
        Assert.Same(services, result);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IMediator) &&
            d.ImplementationType == typeof(Mediator) &&
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
            d.ServiceType == typeof(IRequestHandler<CreateBookCommand, CreateBookResponse>) &&
            d.ImplementationType == typeof(CreateBookCommandHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(INotificationHandler<BookCreatedDomainEvent>) &&
            d.ImplementationType == typeof(BookCreatedDomainEventHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IValidator<CreateBookCommand>) &&
            d.ImplementationType == typeof(CreateBookCommandValidator) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }
}
