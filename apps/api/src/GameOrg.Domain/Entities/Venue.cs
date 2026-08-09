using NetTopologySuite.Geometries;

namespace GameOrg.Domain.Entities;

public sealed class Venue
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Slug { get; set; }
    public required Dictionary<string, string> NameI18n { get; set; }
    public Dictionary<string, string>? DescriptionI18n { get; set; }
    public Dictionary<string, string>? AddressI18n { get; set; }
    public Guid? CityId { get; set; }
    public City? City { get; set; }
    /// <summary>geography(Point,4326). X = долгота, Y = широта.</summary>
    public required Point Location { get; set; }
    public bool? IsIndoor { get; set; }
    public VenueSurface? Surface { get; set; }
    public bool HasLighting { get; set; }
    public bool HasShowers { get; set; }
    public bool HasParking { get; set; }
    public bool HasTribunes { get; set; }
    /// <summary>Ориентир за час.</summary>
    public decimal? PriceHint { get; set; }
    public string Currency { get; set; } = "AZN";
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public Dictionary<string, object>? OpeningHours { get; set; }
    public VenueStatus Status { get; set; } = VenueStatus.Published;
    /// <summary>Дедупликация каталога.</summary>
    public Guid? MergedIntoId { get; set; }

    public decimal? RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public int EventsCount { get; set; }

    /// <summary>
    /// Generated-колонка Postgres (STORED, склейка name/address по az/ru/en) —
    /// заполняется базой, из кода никогда не пишем. Используется GIN-индексом
    /// venues_search_trgm; сам поиск по имени в GET /api/venues пока не подключён.
    /// </summary>
    public string? SearchText { get; private set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<VenueSport> Sports { get; set; } = new List<VenueSport>();
    public ICollection<VenuePhoto> Photos { get; set; } = new List<VenuePhoto>();
    public ICollection<VenueReview> Reviews { get; set; } = new List<VenueReview>();
}
