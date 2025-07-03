namespace TraSuaApp.Domain.Entities;

public class LichSuNhapXuatKho
{
    public Guid Id { get; set; }
    public DateTime ThoiGian { get; set; }
    public Guid IdNguyenLieu { get; set; }
    public decimal SoLuong { get; set; }
    public string Loai { get; set; } = string.Empty; // "Nhap" hoặc "Xuat"
    public string? GhiChu { get; set; }

    public NguyenLieu NguyenLieu { get; set; }
}