namespace TraSuaApp.Shared.Dtos;

public class ToppingDto : DtoBase
{
    public override string ApiRoute => "Topping";
    public decimal Gia { get; set; }
    public bool NgungBan { get; set; }

    public List<Guid> IdNhomSanPhams { get; set; } = new();

}