using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class ToppingDto : DtoBase
{
    public override string ApiRoute => "Topping";
    public decimal Gia { get; set; }
    public bool NgungBan { get; set; }

    public virtual List<Guid> NhomSanPhams { get; set; } = new List<Guid>();
    public int SoLuong { get; set; }
    public string TimKiem =>
       $"{Ten?.ToLower() ?? ""} " +
       TextSearchHelper.NormalizeText(Ten ?? "") + " " +
       TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
       TextSearchHelper.GetShortName(Ten ?? "");

}

