namespace TraSuaApp.Domain.Entities;

public partial class Location
{
    public Guid Id { get; set; }

    public string StartAddress { get; set; } = string.Empty;
    public double? StartLat { get; set; }
    public double? StartLong { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? MoneyDistance { get; set; }
    public string? Matrix { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

}
