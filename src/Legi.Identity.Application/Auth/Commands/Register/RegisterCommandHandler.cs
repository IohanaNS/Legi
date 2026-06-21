using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecureTokenFactory _tokenFactory;
    private readonly IEmailSender _emailSender;
    private readonly EmailConfirmationSettings _emailConfirmationSettings;
    private readonly TurnstileSettings _turnstileSettings;
    private readonly IHumanVerificationService _humanVerificationService;
    private readonly IBreachedPasswordChecker _breachedPasswordChecker;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISecureTokenFactory tokenFactory,
        IEmailSender emailSender,
        EmailConfirmationSettings emailConfirmationSettings,
        TurnstileSettings turnstileSettings,
        IHumanVerificationService humanVerificationService,
        IBreachedPasswordChecker breachedPasswordChecker,
        ISecurityAuditLogger auditLogger,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenFactory = tokenFactory;
        _emailSender = emailSender;
        _emailConfirmationSettings = emailConfirmationSettings;
        _turnstileSettings = turnstileSettings;
        _humanVerificationService = humanVerificationService;
        _breachedPasswordChecker = breachedPasswordChecker;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (_turnstileSettings.Enabled &&
            _turnstileSettings.RequireForRegistration &&
            !await _humanVerificationService.VerifyAsync(
                request.TurnstileToken,
                request.RemoteIpAddress,
                HumanVerificationActions.Register,
                cancellationToken))
        {
            throw new HumanVerificationRequiredException();
        }

        var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUserByEmail != null)
            throw new ConflictException("A user with this email already exists.");

        var existingUserByUsername = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUserByUsername != null)
            throw new ConflictException("A user with this username already exists.");

        if (await _breachedPasswordChecker.IsBreachedAsync(request.Password, cancellationToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Password),
                    "This password has appeared in a data breach elsewhere on the internet and is not safe to use. Please choose a different one.")
            ]);
        }

        var email = Email.Create(request.Email);
        var username = Username.Create(request.Username);
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(email, username, passwordHash);

        var (rawToken, tokenHash) = _tokenFactory.Create();
        var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_emailConfirmationSettings.TokenLifetimeMinutes);
        user.AddEmailConfirmationToken(tokenHash, tokenExpiresAt);

        await _userRepository.AddAsync(user, cancellationToken);

        _auditLogger.Record(new SecurityAuditEvent(
            SecurityEventType.AccountRegistered,
            UserId: user.Id,
            IpAddress: request.RemoteIpAddress));

        await TrySendConfirmationEmailAsync(user, rawToken, tokenHash, request.Language, cancellationToken);

        return new RegisterResponse(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            EmailConfirmationRequired: true
        );
    }

    private async Task TrySendConfirmationEmailAsync(
        User user,
        string rawToken,
        string tokenHash,
        string? language,
        CancellationToken cancellationToken)
    {
        try
        {
            var confirmationUrl =
                $"{_emailConfirmationSettings.FrontendBaseUrl.TrimEnd('/')}/confirm-email?token={WebUtility.UrlEncode(rawToken)}";

            var content = EmailConfirmationEmailTemplate.Build(
                user.Username.Value,
                confirmationUrl,
                _emailConfirmationSettings.TokenLifetimeMinutes,
                language);

            await _emailSender.SendAsync(user.Email.Value, content, cancellationToken);
            user.MarkEmailConfirmationTokenSent(
                tokenHash,
                DateTime.UtcNow);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to send confirmation email for user {UserId}", user.Id);
        }
    }
}
