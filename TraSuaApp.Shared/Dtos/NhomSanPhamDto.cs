using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class NhomSanPhamDto : DtoBase
{
    public override string ApiRoute => "NhomSanPham";
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}
