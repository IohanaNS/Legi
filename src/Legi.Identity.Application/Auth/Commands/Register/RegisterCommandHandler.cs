using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.Identity.Domain.ValueObjects;

namespace Legi.Identity.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUserByEmail != null)
            throw new ConflictException("A user with this email already exists.");

        var existingUserByUsername = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUserByUsername != null)
            throw new ConflictException("A user with this username already exists.");

        var email = Email.Create(request.Email);
        var username = Username.Create(request.Username);
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(email, username, passwordHash);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshTokenHash = _tokenService.GenerateRefreshToken();

        user.AddRefreshToken(refreshTokenHash, DateTime.UtcNow.AddDays(7));

        await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterResponse(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            accessToken,
            refreshTokenHash,
            expiresAt
        );
    }
}
