using Microsoft.AspNetCore.Identity;
using Skolio.Identity.Domain.Enums;

namespace Skolio.Identity.Infrastructure.Auth;

public sealed class SkolioIdentityUser : IdentityUser
{
    public IdentityAccountLifecycleStatus AccountLifecycleStatus { get; set; } = IdentityAccountLifecycleStatus.PendingActivation;
    public DateTimeOffset? ActivationRequestedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
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
