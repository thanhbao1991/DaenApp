using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class TuDienTraCuuDto : DtoBase
{
    public override string ApiRoute => "TuDienTraCuu";
    public string TenPhienDich { get; set; } = null!;

    public bool DangSuDung { get; set; }

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.NormalizeText(Ten ?? "") + " " +
        StringHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}

