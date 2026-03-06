using Mapster;
using MediatR;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Domain.Exceptions;

namespace Skolio.Identity.Application.Profiles;

public sealed class UpsertUserProfileCommandHandler(IIdentityCommandStore commandStore, IIdentityReadStore readStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<UpsertUserProfileCommand, UserProfileContract>
{
    public async Task<UserProfileContract> Handle(UpsertUserProfileCommand request, CancellationToken cancellationToken)
    {
        UserProfile profile;
        if (request.UserProfileId is Guid profileId)
        {
            profile = await readStore.GetUserProfileAsync(profileId, cancellationToken) ?? throw new IdentityDomainException("User profile was not found.");
            profile.Update(request.FirstName, request.LastName, request.UserType, request.Email);
        }
        else
        {
            profile = UserProfile.Create(Guid.NewGuid(), request.FirstName, request.LastName, request.UserType, request.Email);
        }

        await commandStore.UpsertUserProfileAsync(profile, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return profile.Adapt<UserProfileContract>(mapsterConfig);
    }
}
