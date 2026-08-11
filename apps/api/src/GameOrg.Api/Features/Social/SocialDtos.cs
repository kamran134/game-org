using GameOrg.Domain;

namespace GameOrg.Api.Features.Social;

public sealed record FollowRequest(FollowTargetType TargetType, Guid TargetId);

/// <summary>Общий для «подписчики» (всегда User) и «подписки» (User/Club/Venue вперемешку). Slug — handle у User.</summary>
public sealed record FollowSummaryDto(FollowTargetType TargetType, Guid TargetId, string Slug, string Name, Guid? AvatarId);
