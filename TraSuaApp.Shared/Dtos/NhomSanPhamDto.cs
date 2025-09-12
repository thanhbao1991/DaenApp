using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class NhomSanPhamDto : DtoBase
{
    public override string ApiRoute => "NhomSanPham";
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        TextSearchHelper.NormalizeText(Ten ?? "") + " " +
        TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        TextSearchHelper.GetShortName(Ten ?? "");

}
