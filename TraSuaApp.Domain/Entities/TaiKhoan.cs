namespace TraSuaApp.Domain.Entities;


public class TaiKhoan
{
    public Guid Id { get; set; }

    public string TenDangNhap { get; set; } = default!;

    public string MatKhau { get; set; } = default!;

    public string? TenHienThi { get; set; }

    public string? VaiTro { get; set; } // ví dụ: Admin, NhanVien

    public bool IsActive { get; set; } = true;

    public DateTime ThoiGianTao { get; set; } = DateTime.Now;
}