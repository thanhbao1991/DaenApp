using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class PhuongThucThanhToanDto : DtoBase
{
    public override string ApiRoute => "PhuongThucThanhToan";

    public bool DangSuDung { get; set; }

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        TextSearchHelper.NormalizeText(Ten ?? "") + " " +
        TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        TextSearchHelper.GetShortName(Ten ?? "");

}

