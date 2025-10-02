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
        StringHelper.NormalizeText(Ten ?? "") + " " +
        StringHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}

