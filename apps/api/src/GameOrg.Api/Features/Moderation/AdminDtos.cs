using GameOrg.Domain;

namespace GameOrg.Api.Features.Moderation;

public sealed record AdminOverviewDto(int PendingReports, int PendingVenues, int PendingClaims);

public sealed record AdminUserDto(
    Guid Id,
    string Handle,
    string DisplayName,
    string? AvatarUrl,
    UserRole Role,
    UserStatus Status,
    DateTime CreatedAt);

public sealed record UpdateUserRoleRequest(UserRole Role);
