using GameOrg.Domain;

namespace GameOrg.Api.Features.Social;

public sealed record ActivityActorDto(Guid Id, string Handle, string Name, Guid? AvatarId);

public sealed record ActivityEventDto(Guid Id, string PublicId, string? Title, DateTime StartsAt);

public sealed record ActivityClubDto(Guid Id, string Slug, string Name);

public sealed record ActivityVenueDto(Guid Id, string Slug, string Name);

public sealed record ActivityDto(
    Guid Id,
    ActivityActorDto Actor,
    ActivityVerb Verb,
    ActivityEventDto? Event,
    ActivityClubDto? Club,
    ActivityVenueDto? Venue,
    ActivityActorDto? TargetUser,
    DateTime CreatedAt);
