using Microsoft.AspNetCore.Identity;
using Skolio.Identity.Domain.Enums;

namespace Skolio.Identity.Infrastructure.Auth;

public sealed class SkolioIdentityUser : IdentityUser
{
    public bool IsBootstrapPlatformAdministrator { get; set; }
    public DateTimeOffset? BootstrapMfaCompletedAtUtc { get; set; }
    public DateTimeOffset? BootstrapActivationCompletedAtUtc { get; set; }
    public DateTimeOffset? BootstrapFirstLoginCompletedAtUtc { get; set; }
    public IdentityAccountLifecycleStatus AccountLifecycleStatus { get; set; } = IdentityAccountLifecycleStatus.PendingActivation;
    public IdentityInviteStatus InviteStatus { get; set; } = IdentityInviteStatus.PendingActivation;
    public DateTimeOffset? ActivationRequestedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public DateTimeOffset? InviteSentAtUtc { get; set; }
    public DateTimeOffset? InviteExpiresAtUtc { get; set; }
    public DateTimeOffset? InviteConfirmedAtUtc { get; set; }
    public DateTimeOffset? OnboardingCompletedAtUtc { get; set; }
    public string? InviteTokenHash { get; set; }
    public string? InviteCodeHash { get; set; }
    public DateTimeOffset? DeactivatedAtUtc { get; set; }
    public string? DeactivationReason { get; set; }
    public string? DeactivatedByUserId { get; set; }
    public DateTimeOffset? BlockedAtUtc { get; set; }
    public string? BlockedReason { get; set; }
    public string? BlockedByUserId { get; set; }
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public DateTimeOffset? LastActivityAtUtc { get; set; }
    public DateTimeOffset? InactivityWarningSentAtUtc { get; set; }
}
