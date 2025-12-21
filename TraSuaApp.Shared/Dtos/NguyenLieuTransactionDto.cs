using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class NguyenLieuTransactionDto : DtoBase
{
    public override string ApiRoute => "NguyenLieuTransaction";

    public Guid NguyenLieuId { get; set; }
    public DateTime NgayGio { get; set; }

    public LoaiGiaoDichNguyenLieu Loai { get; set; }
    public decimal SoLuong { get; set; }

    public decimal? DonGia { get; set; }
    public string? GhiChu { get; set; }

    public Guid? ChiTieuHangNgayId { get; set; }
    public Guid? HoaDonId { get; set; }

    // UI/search convenience (optional)
    public string? TenNguyenLieu { get; set; }
    public string? DonViTinh { get; set; }

    public string TimKiem =>
        $"{TenNguyenLieu?.ToLower() ?? ""} " +
        $"{StringHelper.MyNormalizeText(TenNguyenLieu ?? "")} " +
        $"{StringHelper.MyNormalizeText((TenNguyenLieu ?? "").Replace(" ", ""))} " +
        $"{Loai} {SoLuong} {(GhiChu ?? "").ToLower()}";
}
