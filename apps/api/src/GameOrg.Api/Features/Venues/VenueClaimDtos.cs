using GameOrg.Domain;

namespace GameOrg.Api.Features.Venues;

public sealed record CreateVenueClaimRequest(string? Evidence);

public sealed record VenueClaimDto(
    Guid Id,
    Guid VenueId,
    string VenueName,
    Guid UserId,
    string UserDisplayName,
    string? Evidence,
    ReportStatus Status,
    DateTime CreatedAt);
