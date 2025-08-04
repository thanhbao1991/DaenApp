using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Shared.Dtos;

public class PhuongThucThanhToanDto : DtoBase
{
    public override string ApiRoute => "PhuongThucThanhToan";

    public bool DangSuDung { get; set; }

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();

}

