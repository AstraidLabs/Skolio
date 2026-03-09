using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Profiles;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/user-profiles")]
public sealed class UserProfilesController(IMediator mediator, IdentityDbContext dbContext, ILogger<UserProfilesController> logger) : ControllerBase
{
    private static readonly string[] AllowedParentContactChannels =
    [
        "EMAIL",
        "PHONE",
        "APP"
    ];

    private static readonly SchoolPositionOptionContract[] SchoolAdministratorPositionOptions =
    [
        new("SCHOOL_ADMINISTRATOR", "School Administrator"),
        new("DEPUTY_SCHOOL_ADMINISTRATOR", "Deputy School Administrator")
    ];

    private static readonly SchoolPositionOptionContract[] TeacherPositionOptions =
    [
        new("TEACHER", "Teacher"),
        new("CLASS_TEACHER", "Class Teacher"),
        new("SUBJECT_TEACHER", "Subject Teacher")
    ];

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileContract>> Me(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        return profile is null ? NotFound() : Ok(ToContract(profile));
    }

    [HttpGet("me/summary")]
    [Authorize]
    public async Task<ActionResult<MyProfileSummaryContract>> MySummary(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var roleAssignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken);

        var parentStudentLinks = await dbContext.ParentStudentLinks
            .Where(x => x.ParentUserProfileId == actorUserId || x.StudentUserProfileId == actorUserId)
            .OrderBy(x => x.ParentUserProfileId)
            .ThenBy(x => x.StudentUserProfileId)
            .Select(x => new ParentStudentLinkContract(x.Id, x.ParentUserProfileId, x.StudentUserProfileId, x.Relationship))
            .ToListAsync(cancellationToken);

        var schoolIds = roleAssignments.Select(x => x.SchoolId).Distinct().ToList();

        return Ok(new MyProfileSummaryContract(
            ToContract(profile),
            roleAssignments,
            parentStudentLinks,
            schoolIds,
            SchoolScope.IsPlatformAdministrator(User),
            User.IsInRole("SchoolAdministrator"),
            User.IsInRole("Teacher"),
            User.IsInRole("Parent"),
            User.IsInRole("Student")));
    }

    [HttpGet("me/school-position-options")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<SchoolPositionOptionContract>>> MySchoolPositionOptions([FromQuery] Guid? schoolId, CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        if (SchoolScope.IsParent(User) || SchoolScope.IsStudent(User)) return Ok(Array.Empty<SchoolPositionOptionContract>());

        var assignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .Select(x => new { x.SchoolId, x.RoleCode })
            .ToListAsync(cancellationToken);

        var resolvedSchoolId = schoolId ?? assignments.Select(x => x.SchoolId).FirstOrDefault();
        if (resolvedSchoolId == Guid.Empty) return Ok(Array.Empty<SchoolPositionOptionContract>());

        var roleCodes = assignments
            .Where(x => x.SchoolId == resolvedSchoolId)
            .Select(x => x.RoleCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleCodes.Count == 0) return Ok(Array.Empty<SchoolPositionOptionContract>());

        var options = BuildSchoolPositionOptions(roleCodes);
        return Ok(options);
    }

    [HttpGet("{id:guid}/school-position-options")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<SchoolPositionOptionContract>>> UserSchoolPositionOptions(Guid id, [FromQuery] Guid? schoolId, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var query = dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == id);

        if (schoolId.HasValue && schoolId.Value != Guid.Empty)
        {
            query = query.Where(x => x.SchoolId == schoolId.Value);
        }

        var roleCodes = await query
            .Select(x => x.RoleCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return Ok(BuildSchoolPositionOptions(roleCodes));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileContract>> UpdateMe([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var normalizedRequest = NormalizeSelfRequest(profile, request);
        var validationResult = ValidateProfileEditableValues(normalizedRequest);
        if (validationResult is not null) return validationResult;

        var positionTitleChanged = !string.Equals(
            profile.PositionTitle?.Trim(),
            normalizedRequest.PositionTitle?.Trim(),
            StringComparison.OrdinalIgnoreCase);

        if (positionTitleChanged && !string.IsNullOrWhiteSpace(normalizedRequest.PositionTitle))
        {
            var allowedCodes = await ResolveAllowedSchoolPositionCodesForProfile(profile.Id, cancellationToken);
            if (!allowedCodes.Contains(normalizedRequest.PositionTitle))
            {
                return this.ValidationField("positionTitle", "Selected school position is not allowed for current school context.");
            }
        }

        var changedFields = CollectChangedFields(profile, normalizedRequest);

        var result = await mediator.Send(new UpsertUserProfileCommand(
            profile.Id,
            normalizedRequest.FirstName,
            normalizedRequest.LastName,
            profile.UserType,
            profile.Email,
            normalizedRequest.PreferredDisplayName,
            normalizedRequest.PreferredLanguage,
            normalizedRequest.PhoneNumber,
            normalizedRequest.Gender,
            normalizedRequest.DateOfBirth,
            normalizedRequest.NationalIdNumber,
            normalizedRequest.BirthPlace,
            normalizedRequest.PermanentAddress,
            normalizedRequest.CorrespondenceAddress,
            normalizedRequest.ContactEmail,
            normalizedRequest.LegalGuardian1,
            normalizedRequest.LegalGuardian2,
            normalizedRequest.SchoolPlacement,
            normalizedRequest.HealthInsuranceProvider,
            normalizedRequest.Pediatrician,
            normalizedRequest.HealthSafetyNotes,
            normalizedRequest.SupportMeasuresSummary,
            normalizedRequest.PositionTitle,
            normalizedRequest.TeacherRoleLabel,
            normalizedRequest.QualificationSummary,
            normalizedRequest.SchoolContextSummary,
            normalizedRequest.ParentRelationshipSummary,
            normalizedRequest.DeliveryContactName,
            normalizedRequest.DeliveryContactPhone,
            normalizedRequest.PreferredContactChannel,
            normalizedRequest.CommunicationPreferencesSummary,
            normalizedRequest.PublicContactNote,
            normalizedRequest.PreferredContactNote,
            normalizedRequest.AdministrativeWorkDesignation,
            normalizedRequest.AdministrativeOrganizationSummary), cancellationToken);

        Audit("identity.user-profile.self-updated", profile.Id, new { changedFields });
        return Ok(result);
    }

    [HttpGet("linked-students")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<UserProfileContract>>> LinkedStudents(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Parent")) return Forbid();

        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var linkedStudentIds = await dbContext.ParentStudentLinks
            .Where(x => x.ParentUserProfileId == actorUserId)
            .Select(x => x.StudentUserProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (linkedStudentIds.Count == 0) return Ok(Array.Empty<UserProfileContract>());

        var result = await dbContext.UserProfiles
            .Where(x => linkedStudentIds.Contains(x.Id))
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new UserProfileContract(
                x.Id,
                x.FirstName,
                x.LastName,
                x.UserType,
                x.Email,
                x.IsActive,
                x.PreferredDisplayName,
                x.PreferredLanguage,
                x.PhoneNumber,
                x.Gender,
                x.DateOfBirth,
                x.NationalIdNumber,
                x.BirthPlace,
                x.PermanentAddress,
                x.CorrespondenceAddress,
                x.ContactEmail,
                x.LegalGuardian1,
                x.LegalGuardian2,
                x.SchoolPlacement,
                x.HealthInsuranceProvider,
                x.Pediatrician,
                x.HealthSafetyNotes,
                x.SupportMeasuresSummary,
                x.PositionTitle,
                x.TeacherRoleLabel,
                x.QualificationSummary,
                x.SchoolContextSummary,
                x.ParentRelationshipSummary,
                x.DeliveryContactName,
                x.DeliveryContactPhone,
                x.PreferredContactChannel,
                x.CommunicationPreferencesSummary,
                x.PublicContactNote,
                x.PreferredContactNote,
                x.AdministrativeWorkDesignation,
                x.AdministrativeOrganizationSummary))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("student-context")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.StudentSelfService)]
    public async Task<ActionResult<StudentContextContract>> StudentContext(CancellationToken cancellationToken)
    {
        var actorUserId = SchoolScope.ResolveActorUserId(User);
        if (actorUserId == Guid.Empty) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);
        if (profile is null) return NotFound();

        var roleAssignments = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken);

        return Ok(new StudentContextContract(ToContract(profile), roleAssignments));
    }

    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<UserProfileContract>>> List([FromQuery] bool? isActive, [FromQuery] UserType? userType, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.UserProfiles.AsQueryable();

        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            var scopedProfileIds = await dbContext.SchoolRoleAssignments
                .Where(x => scopedSchoolIds.Contains(x.SchoolId))
                .Select(x => x.UserProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => scopedProfileIds.Contains(x.Id));
        }

        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (userType.HasValue) query = query.Where(x => x.UserType == userType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.FirstName, $"%{term}%") || EF.Functions.ILike(x.LastName, $"%{term}%") || EF.Functions.ILike(x.Email, $"%{term}%"));
        }

        var result = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new UserProfileContract(
                x.Id,
                x.FirstName,
                x.LastName,
                x.UserType,
                x.Email,
                x.IsActive,
                x.PreferredDisplayName,
                x.PreferredLanguage,
                x.PhoneNumber,
                x.Gender,
                x.DateOfBirth,
                x.NationalIdNumber,
                x.BirthPlace,
                x.PermanentAddress,
                x.CorrespondenceAddress,
                x.ContactEmail,
                x.LegalGuardian1,
                x.LegalGuardian2,
                x.SchoolPlacement,
                x.HealthInsuranceProvider,
                x.Pediatrician,
                x.HealthSafetyNotes,
                x.SupportMeasuresSummary,
                x.PositionTitle,
                x.TeacherRoleLabel,
                x.QualificationSummary,
                x.SchoolContextSummary,
                x.ParentRelationshipSummary,
                x.DeliveryContactName,
                x.DeliveryContactPhone,
                x.PreferredContactChannel,
                x.CommunicationPreferencesSummary,
                x.PublicContactNote,
                x.PreferredContactNote,
                x.AdministrativeWorkDesignation,
                x.AdministrativeOrganizationSummary))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return profile is null ? NotFound() : Ok(ToContract(profile));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> Update(Guid id, [FromBody] UpdateAdminProfileRequest request, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();

        var normalizedRequest = NormalizeAdminRequest(request);
        var validationResult = ValidateProfileEditableValues(normalizedRequest);
        if (validationResult is not null) return validationResult;

        var changedFields = CollectChangedFields(profile, normalizedRequest);
        var positionTitleChanged = !string.Equals(
            profile.PositionTitle?.Trim(),
            normalizedRequest.PositionTitle?.Trim(),
            StringComparison.OrdinalIgnoreCase);

        if (positionTitleChanged && !string.IsNullOrWhiteSpace(normalizedRequest.PositionTitle))
        {
            var allowedCodes = await ResolveAllowedSchoolPositionCodesForProfile(profile.Id, cancellationToken);
            if (!allowedCodes.Contains(normalizedRequest.PositionTitle))
            {
                return this.ValidationField("positionTitle", "Selected school position is not allowed for current school context.");
            }
        }

        var result = await mediator.Send(new UpsertUserProfileCommand(
            profile.Id,
            normalizedRequest.FirstName,
            normalizedRequest.LastName,
            profile.UserType,
            profile.Email,
            normalizedRequest.PreferredDisplayName,
            normalizedRequest.PreferredLanguage,
            normalizedRequest.PhoneNumber,
            normalizedRequest.Gender,
            normalizedRequest.DateOfBirth,
            normalizedRequest.NationalIdNumber,
            normalizedRequest.BirthPlace,
            normalizedRequest.PermanentAddress,
            normalizedRequest.CorrespondenceAddress,
            normalizedRequest.ContactEmail,
            normalizedRequest.LegalGuardian1,
            normalizedRequest.LegalGuardian2,
            normalizedRequest.SchoolPlacement,
            normalizedRequest.HealthInsuranceProvider,
            normalizedRequest.Pediatrician,
            normalizedRequest.HealthSafetyNotes,
            normalizedRequest.SupportMeasuresSummary,
            normalizedRequest.PositionTitle,
            normalizedRequest.TeacherRoleLabel,
            normalizedRequest.QualificationSummary,
            normalizedRequest.SchoolContextSummary,
            normalizedRequest.ParentRelationshipSummary,
            normalizedRequest.DeliveryContactName,
            normalizedRequest.DeliveryContactPhone,
            normalizedRequest.PreferredContactChannel,
            normalizedRequest.CommunicationPreferencesSummary,
            normalizedRequest.PublicContactNote,
            normalizedRequest.PreferredContactNote,
            normalizedRequest.AdministrativeWorkDesignation,
            normalizedRequest.AdministrativeOrganizationSummary), cancellationToken);

        Audit("identity.user-profile.admin-updated", id, new { changedFields });
        return Ok(result);
    }

    [HttpPut("{id:guid}/activation")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<UserProfileContract>> SetActivation(Guid id, [FromBody] SetActivationRequest request, CancellationToken cancellationToken)
    {
        if (!await HasProfileAccess(id, cancellationToken)) return Forbid();

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();

        if (request.IsActive) profile.Activate(); else profile.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit(request.IsActive ? "identity.user-profile.activated" : "identity.user-profile.deactivated", id, new { request.IsActive });
        return Ok(ToContract(profile));
    }

    private async Task<bool> HasProfileAccess(Guid profileId, CancellationToken cancellationToken)
    {
        if (SchoolScope.IsPlatformAdministrator(User)) return true;

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
        if (scopedSchoolIds.Count == 0) return false;

        return await dbContext.SchoolRoleAssignments.AnyAsync(x => x.UserProfileId == profileId && scopedSchoolIds.Contains(x.SchoolId), cancellationToken);
    }

    private UpdateMyProfileRequest NormalizeSelfRequest(UserProfile profile, UpdateMyProfileRequest request)
    {
        var isStudentOnly = User.IsInRole("Student")
            && !User.IsInRole("Teacher")
            && !User.IsInRole("Parent")
            && !User.IsInRole("SchoolAdministrator")
            && !User.IsInRole("PlatformAdministrator");

        var canEditName = !isStudentOnly;
        var canEditPositionTitle = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator") || User.IsInRole("Teacher");
        var canEditTeacherSection = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator") || User.IsInRole("Teacher");
        var canEditSchoolContextSummary = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator");
        var canEditParentSection = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator") || User.IsInRole("Parent");
        var canEditParentCommunication = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator") || User.IsInRole("Parent");
        var canEditPublicContactNote = User.IsInRole("Teacher") || User.IsInRole("SchoolAdministrator") || User.IsInRole("PlatformAdministrator");
        var canEditPreferredContactNote = User.IsInRole("Parent");
        var canEditAdministrativeEmploymentSection = User.IsInRole("PlatformAdministrator") || User.IsInRole("SchoolAdministrator");

        return request with
        {
            FirstName = canEditName ? request.FirstName : profile.FirstName,
            LastName = canEditName ? request.LastName : profile.LastName,
            Gender = isStudentOnly ? profile.Gender : request.Gender,
            DateOfBirth = isStudentOnly ? profile.DateOfBirth : request.DateOfBirth,
            NationalIdNumber = isStudentOnly ? profile.NationalIdNumber : request.NationalIdNumber,
            BirthPlace = isStudentOnly ? profile.BirthPlace : request.BirthPlace,
            PermanentAddress = isStudentOnly ? profile.PermanentAddress : request.PermanentAddress,
            CorrespondenceAddress = isStudentOnly ? profile.CorrespondenceAddress : request.CorrespondenceAddress,
            ContactEmail = isStudentOnly ? profile.ContactEmail : request.ContactEmail,
            LegalGuardian1 = canEditName ? request.LegalGuardian1 : profile.LegalGuardian1,
            LegalGuardian2 = canEditName ? request.LegalGuardian2 : profile.LegalGuardian2,
            SchoolPlacement = canEditPositionTitle ? request.SchoolPlacement : profile.SchoolPlacement,
            HealthInsuranceProvider = canEditName ? request.HealthInsuranceProvider : profile.HealthInsuranceProvider,
            Pediatrician = canEditName ? request.Pediatrician : profile.Pediatrician,
            HealthSafetyNotes = canEditPositionTitle ? request.HealthSafetyNotes : profile.HealthSafetyNotes,
            SupportMeasuresSummary = canEditPositionTitle ? request.SupportMeasuresSummary : profile.SupportMeasuresSummary,
            PositionTitle = canEditPositionTitle ? request.PositionTitle : profile.PositionTitle,
            TeacherRoleLabel = canEditTeacherSection ? request.TeacherRoleLabel : profile.TeacherRoleLabel,
            QualificationSummary = canEditTeacherSection ? request.QualificationSummary : profile.QualificationSummary,
            SchoolContextSummary = canEditSchoolContextSummary ? request.SchoolContextSummary : profile.SchoolContextSummary,
            ParentRelationshipSummary = canEditParentSection ? request.ParentRelationshipSummary : profile.ParentRelationshipSummary,
            DeliveryContactName = canEditParentSection ? request.DeliveryContactName : profile.DeliveryContactName,
            DeliveryContactPhone = canEditParentSection ? request.DeliveryContactPhone : profile.DeliveryContactPhone,
            PreferredContactChannel = canEditParentCommunication ? request.PreferredContactChannel : profile.PreferredContactChannel,
            CommunicationPreferencesSummary = canEditParentCommunication ? request.CommunicationPreferencesSummary : profile.CommunicationPreferencesSummary,
            PublicContactNote = canEditPublicContactNote ? request.PublicContactNote : profile.PublicContactNote,
            PreferredContactNote = canEditPreferredContactNote ? request.PreferredContactNote : profile.PreferredContactNote,
            AdministrativeWorkDesignation = canEditAdministrativeEmploymentSection ? request.AdministrativeWorkDesignation : profile.AdministrativeWorkDesignation,
            AdministrativeOrganizationSummary = canEditAdministrativeEmploymentSection ? request.AdministrativeOrganizationSummary : profile.AdministrativeOrganizationSummary
        };
    }

    private UpdateAdminProfileRequest NormalizeAdminRequest(UpdateAdminProfileRequest request)
    {
        if (SchoolScope.IsPlatformAdministrator(User))
        {
            return request;
        }

        return request with
        {
            PublicContactNote = null,
            PreferredContactNote = null,
            AdministrativeWorkDesignation = request.AdministrativeWorkDesignation,
            AdministrativeOrganizationSummary = request.AdministrativeOrganizationSummary
        };
    }

    private static IReadOnlyCollection<string> CollectChangedFields(UserProfile profile, ProfileEditableValues request)
    {
        var changed = new List<string>();

        if (!string.Equals(profile.FirstName, request.FirstName, StringComparison.Ordinal)) changed.Add("firstName");
        if (!string.Equals(profile.LastName, request.LastName, StringComparison.Ordinal)) changed.Add("lastName");
        if (!string.Equals(profile.PreferredDisplayName, request.PreferredDisplayName, StringComparison.Ordinal)) changed.Add("preferredDisplayName");
        if (!string.Equals(profile.PreferredLanguage, request.PreferredLanguage, StringComparison.Ordinal)) changed.Add("preferredLanguage");
        if (!string.Equals(profile.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal)) changed.Add("phoneNumber");
        if (!string.Equals(profile.Gender, request.Gender, StringComparison.Ordinal)) changed.Add("gender");
        if (profile.DateOfBirth != request.DateOfBirth) changed.Add("dateOfBirth");
        if (!string.Equals(profile.NationalIdNumber, request.NationalIdNumber, StringComparison.Ordinal)) changed.Add("nationalIdNumber");
        if (!string.Equals(profile.BirthPlace, request.BirthPlace, StringComparison.Ordinal)) changed.Add("birthPlace");
        if (!string.Equals(profile.PermanentAddress, request.PermanentAddress, StringComparison.Ordinal)) changed.Add("permanentAddress");
        if (!string.Equals(profile.CorrespondenceAddress, request.CorrespondenceAddress, StringComparison.Ordinal)) changed.Add("correspondenceAddress");
        if (!string.Equals(profile.ContactEmail, request.ContactEmail, StringComparison.Ordinal)) changed.Add("contactEmail");
        if (!string.Equals(profile.LegalGuardian1, request.LegalGuardian1, StringComparison.Ordinal)) changed.Add("legalGuardian1");
        if (!string.Equals(profile.LegalGuardian2, request.LegalGuardian2, StringComparison.Ordinal)) changed.Add("legalGuardian2");
        if (!string.Equals(profile.SchoolPlacement, request.SchoolPlacement, StringComparison.Ordinal)) changed.Add("schoolPlacement");
        if (!string.Equals(profile.HealthInsuranceProvider, request.HealthInsuranceProvider, StringComparison.Ordinal)) changed.Add("healthInsuranceProvider");
        if (!string.Equals(profile.Pediatrician, request.Pediatrician, StringComparison.Ordinal)) changed.Add("pediatrician");
        if (!string.Equals(profile.HealthSafetyNotes, request.HealthSafetyNotes, StringComparison.Ordinal)) changed.Add("healthSafetyNotes");
        if (!string.Equals(profile.SupportMeasuresSummary, request.SupportMeasuresSummary, StringComparison.Ordinal)) changed.Add("supportMeasuresSummary");
        if (!string.Equals(profile.PositionTitle, request.PositionTitle, StringComparison.Ordinal)) changed.Add("positionTitle");
        if (!string.Equals(profile.TeacherRoleLabel, request.TeacherRoleLabel, StringComparison.Ordinal)) changed.Add("teacherRoleLabel");
        if (!string.Equals(profile.QualificationSummary, request.QualificationSummary, StringComparison.Ordinal)) changed.Add("qualificationSummary");
        if (!string.Equals(profile.SchoolContextSummary, request.SchoolContextSummary, StringComparison.Ordinal)) changed.Add("schoolContextSummary");
        if (!string.Equals(profile.ParentRelationshipSummary, request.ParentRelationshipSummary, StringComparison.Ordinal)) changed.Add("parentRelationshipSummary");
        if (!string.Equals(profile.DeliveryContactName, request.DeliveryContactName, StringComparison.Ordinal)) changed.Add("deliveryContactName");
        if (!string.Equals(profile.DeliveryContactPhone, request.DeliveryContactPhone, StringComparison.Ordinal)) changed.Add("deliveryContactPhone");
        if (!string.Equals(profile.PreferredContactChannel, request.PreferredContactChannel, StringComparison.Ordinal)) changed.Add("preferredContactChannel");
        if (!string.Equals(profile.CommunicationPreferencesSummary, request.CommunicationPreferencesSummary, StringComparison.Ordinal)) changed.Add("communicationPreferencesSummary");
        if (!string.Equals(profile.PublicContactNote, request.PublicContactNote, StringComparison.Ordinal)) changed.Add("publicContactNote");
        if (!string.Equals(profile.PreferredContactNote, request.PreferredContactNote, StringComparison.Ordinal)) changed.Add("preferredContactNote");
        if (!string.Equals(profile.AdministrativeWorkDesignation, request.AdministrativeWorkDesignation, StringComparison.Ordinal)) changed.Add("administrativeWorkDesignation");
        if (!string.Equals(profile.AdministrativeOrganizationSummary, request.AdministrativeOrganizationSummary, StringComparison.Ordinal)) changed.Add("administrativeOrganizationSummary");

        return changed;
    }


    private ActionResult? ValidateProfileEditableValues(ProfileEditableValues values)
    {
        if (values.DateOfBirth is not null && values.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            return this.ValidationField("dateOfBirth", "Date of birth cannot be in the future.");
        if (!string.IsNullOrWhiteSpace(values.ContactEmail) && !values.ContactEmail.Contains('@', StringComparison.Ordinal))
            return this.ValidationField("contactEmail", "Contact email format is invalid.");
        if (!string.IsNullOrWhiteSpace(values.NationalIdNumber) && values.NationalIdNumber.Length > 32)
            return this.ValidationField("nationalIdNumber", "National ID is too long.");
        if (!string.IsNullOrWhiteSpace(values.HealthSafetyNotes) && values.HealthSafetyNotes.Length > 1000)
            return this.ValidationField("healthSafetyNotes", "Health and safety notes are too long.");
        if (!string.IsNullOrWhiteSpace(values.SupportMeasuresSummary) && values.SupportMeasuresSummary.Length > 1000)
            return this.ValidationField("supportMeasuresSummary", "Support measures summary is too long.");
        if (!string.IsNullOrWhiteSpace(values.TeacherRoleLabel) && values.TeacherRoleLabel.Length > 120)
            return this.ValidationField("teacherRoleLabel", "Teacher role label is too long.");
        if (!string.IsNullOrWhiteSpace(values.QualificationSummary) && values.QualificationSummary.Length > 1000)
            return this.ValidationField("qualificationSummary", "Qualification summary is too long.");
        if (!string.IsNullOrWhiteSpace(values.SchoolContextSummary) && values.SchoolContextSummary.Length > 1000)
            return this.ValidationField("schoolContextSummary", "School context summary is too long.");
        if (!string.IsNullOrWhiteSpace(values.ParentRelationshipSummary) && values.ParentRelationshipSummary.Length > 500)
            return this.ValidationField("parentRelationshipSummary", "Parent relationship summary is too long.");
        if (!string.IsNullOrWhiteSpace(values.DeliveryContactName) && values.DeliveryContactName.Length > 160)
            return this.ValidationField("deliveryContactName", "Delivery contact name is too long.");
        if (!string.IsNullOrWhiteSpace(values.DeliveryContactPhone) && values.DeliveryContactPhone.Length > 32)
            return this.ValidationField("deliveryContactPhone", "Delivery contact phone is too long.");
        if (!string.IsNullOrWhiteSpace(values.CommunicationPreferencesSummary) && values.CommunicationPreferencesSummary.Length > 500)
            return this.ValidationField("communicationPreferencesSummary", "Communication preferences summary is too long.");
        if (!string.IsNullOrWhiteSpace(values.AdministrativeWorkDesignation) && values.AdministrativeWorkDesignation.Length > 120)
            return this.ValidationField("administrativeWorkDesignation", "Administrative work designation is too long.");
        if (!string.IsNullOrWhiteSpace(values.AdministrativeOrganizationSummary) && values.AdministrativeOrganizationSummary.Length > 500)
            return this.ValidationField("administrativeOrganizationSummary", "Administrative organization summary is too long.");
        if (!string.IsNullOrWhiteSpace(values.PreferredContactChannel)
            && !AllowedParentContactChannels.Contains(values.PreferredContactChannel, StringComparer.OrdinalIgnoreCase))
            return this.ValidationField("preferredContactChannel", "Preferred contact channel is invalid.");

        return null;
    }

    private static UserProfileContract ToContract(UserProfile profile)
        => new(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.UserType,
            profile.Email,
            profile.IsActive,
            profile.PreferredDisplayName,
            profile.PreferredLanguage,
            profile.PhoneNumber,
            profile.Gender,
            profile.DateOfBirth,
            profile.NationalIdNumber,
            profile.BirthPlace,
            profile.PermanentAddress,
            profile.CorrespondenceAddress,
            profile.ContactEmail,
            profile.LegalGuardian1,
            profile.LegalGuardian2,
            profile.SchoolPlacement,
            profile.HealthInsuranceProvider,
            profile.Pediatrician,
            profile.HealthSafetyNotes,
            profile.SupportMeasuresSummary,
            profile.PositionTitle,
            profile.TeacherRoleLabel,
            profile.QualificationSummary,
            profile.SchoolContextSummary,
            profile.ParentRelationshipSummary,
            profile.DeliveryContactName,
            profile.DeliveryContactPhone,
            profile.PreferredContactChannel,
            profile.CommunicationPreferencesSummary,
            profile.PublicContactNote,
            profile.PreferredContactNote,
            profile.AdministrativeWorkDesignation,
            profile.AdministrativeOrganizationSummary);

    private async Task<HashSet<string>> ResolveAllowedSchoolPositionCodesForProfile(Guid profileId, CancellationToken cancellationToken)
    {
        var roleCodes = await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == profileId)
            .Select(x => x.RoleCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return BuildSchoolPositionOptions(roleCodes)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<SchoolPositionOptionContract> BuildSchoolPositionOptions(IEnumerable<string> roleCodes)
    {
        var set = roleCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var options = new List<SchoolPositionOptionContract>();

        if (set.Contains("SchoolAdministrator"))
        {
            options.AddRange(SchoolAdministratorPositionOptions);
        }

        if (set.Contains("Teacher"))
        {
            options.AddRange(TeacherPositionOptions);
        }

        return options
            .DistinctBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record UpsertUserProfileRequest(Guid? UserProfileId, string FirstName, string LastName, UserType UserType, string Email);

    public sealed record UpdateMyProfileRequest(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? Gender,
        DateOnly? DateOfBirth,
        string? NationalIdNumber,
        string? BirthPlace,
        string? PermanentAddress,
        string? CorrespondenceAddress,
        string? ContactEmail,
        string? LegalGuardian1,
        string? LegalGuardian2,
        string? SchoolPlacement,
        string? HealthInsuranceProvider,
        string? Pediatrician,
        string? HealthSafetyNotes,
        string? SupportMeasuresSummary,
        string? PositionTitle,
        string? TeacherRoleLabel,
        string? QualificationSummary,
        string? SchoolContextSummary,
        string? ParentRelationshipSummary,
        string? DeliveryContactName,
        string? DeliveryContactPhone,
        string? PreferredContactChannel,
        string? CommunicationPreferencesSummary,
        string? PublicContactNote,
        string? PreferredContactNote,
        string? AdministrativeWorkDesignation,
        string? AdministrativeOrganizationSummary) : ProfileEditableValues(FirstName, LastName, PreferredDisplayName, PreferredLanguage, PhoneNumber, Gender, DateOfBirth, NationalIdNumber, BirthPlace, PermanentAddress, CorrespondenceAddress, ContactEmail, LegalGuardian1, LegalGuardian2, SchoolPlacement, HealthInsuranceProvider, Pediatrician, HealthSafetyNotes, SupportMeasuresSummary, PositionTitle, TeacherRoleLabel, QualificationSummary, SchoolContextSummary, ParentRelationshipSummary, DeliveryContactName, DeliveryContactPhone, PreferredContactChannel, CommunicationPreferencesSummary, PublicContactNote, PreferredContactNote, AdministrativeWorkDesignation, AdministrativeOrganizationSummary);

    public sealed record UpdateAdminProfileRequest(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? Gender,
        DateOnly? DateOfBirth,
        string? NationalIdNumber,
        string? BirthPlace,
        string? PermanentAddress,
        string? CorrespondenceAddress,
        string? ContactEmail,
        string? LegalGuardian1,
        string? LegalGuardian2,
        string? SchoolPlacement,
        string? HealthInsuranceProvider,
        string? Pediatrician,
        string? HealthSafetyNotes,
        string? SupportMeasuresSummary,
        string? PositionTitle,
        string? TeacherRoleLabel,
        string? QualificationSummary,
        string? SchoolContextSummary,
        string? ParentRelationshipSummary,
        string? DeliveryContactName,
        string? DeliveryContactPhone,
        string? PreferredContactChannel,
        string? CommunicationPreferencesSummary,
        string? PublicContactNote,
        string? PreferredContactNote,
        string? AdministrativeWorkDesignation,
        string? AdministrativeOrganizationSummary) : ProfileEditableValues(FirstName, LastName, PreferredDisplayName, PreferredLanguage, PhoneNumber, Gender, DateOfBirth, NationalIdNumber, BirthPlace, PermanentAddress, CorrespondenceAddress, ContactEmail, LegalGuardian1, LegalGuardian2, SchoolPlacement, HealthInsuranceProvider, Pediatrician, HealthSafetyNotes, SupportMeasuresSummary, PositionTitle, TeacherRoleLabel, QualificationSummary, SchoolContextSummary, ParentRelationshipSummary, DeliveryContactName, DeliveryContactPhone, PreferredContactChannel, CommunicationPreferencesSummary, PublicContactNote, PreferredContactNote, AdministrativeWorkDesignation, AdministrativeOrganizationSummary);

    public abstract record ProfileEditableValues(
        string FirstName,
        string LastName,
        string? PreferredDisplayName,
        string? PreferredLanguage,
        string? PhoneNumber,
        string? Gender,
        DateOnly? DateOfBirth,
        string? NationalIdNumber,
        string? BirthPlace,
        string? PermanentAddress,
        string? CorrespondenceAddress,
        string? ContactEmail,
        string? LegalGuardian1,
        string? LegalGuardian2,
        string? SchoolPlacement,
        string? HealthInsuranceProvider,
        string? Pediatrician,
        string? HealthSafetyNotes,
        string? SupportMeasuresSummary,
        string? PositionTitle,
        string? TeacherRoleLabel,
        string? QualificationSummary,
        string? SchoolContextSummary,
        string? ParentRelationshipSummary,
        string? DeliveryContactName,
        string? DeliveryContactPhone,
        string? PreferredContactChannel,
        string? CommunicationPreferencesSummary,
        string? PublicContactNote,
        string? PreferredContactNote,
        string? AdministrativeWorkDesignation,
        string? AdministrativeOrganizationSummary);

    public sealed record SetActivationRequest(bool IsActive);
    public sealed record StudentContextContract(UserProfileContract Profile, IReadOnlyCollection<SchoolRoleAssignmentContract> RoleAssignments);
    public sealed record SchoolPositionOptionContract(string Code, string Label);

    public sealed record MyProfileSummaryContract(
        UserProfileContract Profile,
        IReadOnlyCollection<SchoolRoleAssignmentContract> RoleAssignments,
        IReadOnlyCollection<ParentStudentLinkContract> ParentStudentLinks,
        IReadOnlyCollection<Guid> SchoolIds,
        bool IsPlatformAdministrator,
        bool IsSchoolAdministrator,
        bool IsTeacher,
        bool IsParent,
        bool IsStudent);
}

