using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class CongThucDto : DtoBase
{
    public override string ApiRoute => "CongThuc";

    public Guid SanPhamBienTheId { get; set; }

    public string? Loai { get; set; }
    public bool IsDefault { get; set; }

    public string? TenSanPham { get; set; }
    public string? TenBienThe { get; set; }


    // tiện cho search
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        $"{TenSanPham?.ToLower() ?? ""} " +
        $"{TenBienThe?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");
}