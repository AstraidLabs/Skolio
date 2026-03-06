using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Domain.Exceptions;

namespace Skolio.Identity.Domain.Entities;

public sealed class UserProfile
{
    private UserProfile(Guid id, string firstName, string lastName, UserType userType, string email)
    {
        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UserType = userType;
        Email = email.Trim();
    }

    public Guid Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public UserType UserType { get; private set; }
    public string Email { get; private set; }

    public static UserProfile Create(Guid id, string firstName, string lastName, UserType userType, string email)
    {
        if (id == Guid.Empty)
            throw new IdentityDomainException("User profile id is required.");
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            throw new IdentityDomainException("User profile first name, last name and email are required.");

        return new UserProfile(id, firstName, lastName, userType, email);
    }

    public void Update(string firstName, string lastName, UserType userType, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            throw new IdentityDomainException("User profile first name, last name and email are required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UserType = userType;
        Email = email.Trim();
    }
}
