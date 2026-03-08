using MediatR;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Domain.Enums;

namespace Skolio.Identity.Application.Profiles;

public sealed record UpsertUserProfileCommand(
    Guid? UserProfileId,
    string FirstName,
    string LastName,
    UserType UserType,
    string Email,
    string? PreferredDisplayName,
    string? PreferredLanguage,
    string? PhoneNumber,
    string? PositionTitle,
    string? PublicContactNote,
    string? PreferredContactNote) : IRequest<UserProfileContract>;
