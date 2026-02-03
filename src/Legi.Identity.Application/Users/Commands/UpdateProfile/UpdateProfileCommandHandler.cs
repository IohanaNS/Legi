using Legi.Identity.Domain.Repositories;
using MediatR;

namespace Legi.Identity.Application.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(IUserRepository userRepository)
    : IRequestHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    public async Task<UpdateProfileResponse> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new ApplicationException("USER_NOT_FOUND");

        user.UpdateProfile(request.Name, request.Bio, request.AvatarUrl);

        await userRepository.UpdateAsync(user, cancellationToken);

        return new UpdateProfileResponse(
            user.Id,
            user.Name,
            user.Bio,
            user.AvatarUrl
        );
    }
}