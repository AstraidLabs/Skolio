using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Domain.Exceptions;

namespace Skolio.Identity.Domain.Entities;

public sealed class UserProfile
{
    private UserProfile(
        Guid id,
        string firstName,
        string lastName,
        UserType userType,
        string email,
        string? preferredDisplayName,
        string? preferredLanguage,
        string? phoneNumber,
        string? positionTitle,
        string? publicContactNote,
        string? preferredContactNote)
    {
        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UserType = userType;
        Email = email.Trim();
        PreferredDisplayName = NormalizeOptional(preferredDisplayName);
        PreferredLanguage = NormalizeOptional(preferredLanguage);
        PhoneNumber = NormalizeOptional(phoneNumber);
        PositionTitle = NormalizeOptional(positionTitle);
        PublicContactNote = NormalizeOptional(publicContactNote);
        PreferredContactNote = NormalizeOptional(preferredContactNote);
        IsActive = true;
    }

    public Guid Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public UserType UserType { get; private set; }
    public string Email { get; private set; }
    public string? PreferredDisplayName { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? PositionTitle { get; private set; }
    public string? PublicContactNote { get; private set; }
    public string? PreferredContactNote { get; private set; }
    public bool IsActive { get; private set; }

    public static UserProfile Create(
        Guid id,
        string firstName,
        string lastName,
        UserType userType,
        string email,
        string? preferredDisplayName = null,
        string? preferredLanguage = null,
        string? phoneNumber = null,
        string? positionTitle = null,
        string? publicContactNote = null,
        string? preferredContactNote = null)
    {
        if (id == Guid.Empty)
            throw new IdentityDomainException("User profile id is required.");
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            throw new IdentityDomainException("User profile first name, last name and email are required.");

        return new UserProfile(
            id,
            firstName,
            lastName,
            userType,
            email,
            preferredDisplayName,
            preferredLanguage,
            phoneNumber,
            positionTitle,
            publicContactNote,
            preferredContactNote);
    }

    public void Update(
        string firstName,
        string lastName,
        UserType userType,
        string email,
        string? preferredDisplayName,
        string? preferredLanguage,
        string? phoneNumber,
        string? positionTitle,
        string? publicContactNote,
        string? preferredContactNote)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            throw new IdentityDomainException("User profile first name, last name and email are required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UserType = userType;
        Email = email.Trim();
        PreferredDisplayName = NormalizeOptional(preferredDisplayName);
        PreferredLanguage = NormalizeOptional(preferredLanguage);
        PhoneNumber = NormalizeOptional(phoneNumber);
        PositionTitle = NormalizeOptional(positionTitle);
        PublicContactNote = NormalizeOptional(publicContactNote);
        PreferredContactNote = NormalizeOptional(preferredContactNote);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
