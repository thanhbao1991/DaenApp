namespace TraSuaApp.Shared.Dtos;

public class TaiKhoanDto
{

    public int STT { get; set; }
    public Guid Id { get; set; }
    public string TenDangNhap { get; set; } = default!;
    public string? MatKhau { get; set; } = default!;

    public string? TenHienThi { get; set; }
    public string? VaiTro { get; set; }
    public bool IsActive { get; set; }
    public DateTime ThoiGianTao { get; set; }
    public string? TenNormalized { get; set; }

}