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
            profile.Update(
                request.FirstName,
                request.LastName,
                request.UserType,
                request.Email,
                request.PreferredDisplayName,
                request.PreferredLanguage,
                request.PhoneNumber,
                request.Gender,
                request.DateOfBirth,
                request.NationalIdNumber,
                request.BirthPlace,
                request.PermanentAddress,
                request.CorrespondenceAddress,
                request.ContactEmail,
                request.LegalGuardian1,
                request.LegalGuardian2,
                request.SchoolPlacement,
                request.HealthInsuranceProvider,
                request.Pediatrician,
                request.HealthSafetyNotes,
                request.SupportMeasuresSummary,
                request.PositionTitle,
                request.TeacherRoleLabel,
                request.QualificationSummary,
                request.SchoolContextSummary,
                request.ParentRelationshipSummary,
                request.DeliveryContactName,
                request.DeliveryContactPhone,
                request.PreferredContactChannel,
                request.CommunicationPreferencesSummary,
                request.PublicContactNote,
                request.PreferredContactNote,
                request.AdministrativeWorkDesignation,
                request.AdministrativeOrganizationSummary);
        }
        else
        {
            profile = UserProfile.Create(
                Guid.NewGuid(),
                request.FirstName,
                request.LastName,
                request.UserType,
                request.Email,
                request.PreferredDisplayName,
                request.PreferredLanguage,
                request.PhoneNumber,
                request.Gender,
                request.DateOfBirth,
                request.NationalIdNumber,
                request.BirthPlace,
                request.PermanentAddress,
                request.CorrespondenceAddress,
                request.ContactEmail,
                request.LegalGuardian1,
                request.LegalGuardian2,
                request.SchoolPlacement,
                request.HealthInsuranceProvider,
                request.Pediatrician,
                request.HealthSafetyNotes,
                request.SupportMeasuresSummary,
                request.PositionTitle,
                request.TeacherRoleLabel,
                request.QualificationSummary,
                request.SchoolContextSummary,
                request.ParentRelationshipSummary,
                request.DeliveryContactName,
                request.DeliveryContactPhone,
                request.PreferredContactChannel,
                request.CommunicationPreferencesSummary,
                request.PublicContactNote,
                request.PreferredContactNote,
                request.AdministrativeWorkDesignation,
                request.AdministrativeOrganizationSummary);
        }

        await commandStore.UpsertUserProfileAsync(profile, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return profile.Adapt<UserProfileContract>(mapsterConfig);
    }
}
