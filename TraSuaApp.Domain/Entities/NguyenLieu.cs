namespace TraSuaApp.Domain.Entities;

public class NguyenLieu
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? DonViTinh { get; set; }
    public decimal TonKho { get; set; }
    public decimal? GiaNhap { get; set; }
    public bool DangSuDung { get; set; } = true;

    public ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; }
    public ICollection<ChiTietHoaDonNhap> LichSuNhap { get; set; }
}