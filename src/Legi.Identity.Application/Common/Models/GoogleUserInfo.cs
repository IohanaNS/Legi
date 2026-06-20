namespace Legi.Identity.Application.Common.Models;

public record GoogleUserInfo(
    string Sub,
    string Email,
    bool EmailVerified,
    string? Name,
    string? Picture);
