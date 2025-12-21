using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class NguyenLieuBanHangDto : DtoBase
{
    public override string ApiRoute => "NguyenLieuBanHang";

    public bool DangSuDung { get; set; }
    public string? DonViTinh { get; set; }

    /// <summary>
    /// Tồn kho theo đơn vị bán (lon/ml/gram...)
    /// </summary>
    public decimal TonKho { get; set; }


    //
    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}

