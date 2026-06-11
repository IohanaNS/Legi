using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Storage;

namespace Legi.Social.Application.Profiles.Commands.SetProfileImage;

/// <summary>
/// Persists the public URL of an already-uploaded profile image onto the
/// user's profile. The binary upload (validation, processing, storage) happens
/// in the API layer before this command is sent — only the resulting URL flows
/// through the mediator, keeping the request log-safe.
/// </summary>
public record SetProfileImageCommand(
    Guid UserId,
    ProfileImageKind Kind,
    string Url) : IRequest<SetProfileImageResponse>;
