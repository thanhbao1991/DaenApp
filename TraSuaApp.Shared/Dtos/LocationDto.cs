namespace TraSuaApp.Shared.Dtos;

public class LocationDto : DtoBase
{
    public override string ApiRoute => "Location";

    public string StartAddress { get; set; } = string.Empty;
    public double? StartLat { get; set; }
    public double? StartLong { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? MoneyDistance { get; set; }
    public string? Matrix { get; set; }

}

