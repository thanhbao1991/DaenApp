using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class NhomSanPhamDto : DtoBase
{
    public override string ApiRoute => "NhomSanPham";
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.NormalizeText(Ten ?? "") + " " +
        StringHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}
