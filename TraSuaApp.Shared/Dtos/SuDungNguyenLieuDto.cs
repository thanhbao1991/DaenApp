using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class SuDungNguyenLieuDto : DtoBase
{
    public override string ApiRoute => "SuDungNguyenLieu";

    public Guid CongThucId { get; set; }
    public Guid NguyenLieuId { get; set; }
    public decimal SoLuong { get; set; }

    // Audit

    // Thông tin hiển thị
    public string? TenSanPham { get; set; }
    public string? TenBienThe { get; set; }
    public string? TenNguyenLieu { get; set; }
    public string? DonViTinh { get; set; }
    public string? GhiChu { get; set; }


    public string TimKiem =>
        $"{TenSanPham?.ToLower() ?? ""} " +
        $"{TenBienThe?.ToLower() ?? ""} " +
        $"{TenNguyenLieu?.ToLower() ?? ""} " +
        $"{StringHelper.MyNormalizeText(TenSanPham ?? "")} " +
        $"{StringHelper.MyNormalizeText(TenBienThe ?? "")} " +
        $"{StringHelper.MyNormalizeText(TenNguyenLieu ?? "")}";
}