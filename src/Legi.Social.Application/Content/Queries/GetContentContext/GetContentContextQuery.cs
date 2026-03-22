using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Application.Content.Queries.GetContentContext;

/// <summary>
/// Gets enriched context about interactable content.
/// Used as a "header" on comments and likes pages — shows what content
/// people are interacting with, without calling other services.
/// </summary>
public record GetContentContextQuery(
    InteractableType TargetType,
    Guid TargetId) : IRequest<ContentContextDto>;