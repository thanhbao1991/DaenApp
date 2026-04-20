namespace TraSuaApp.Infrastructure.Entities;

public partial class TaiKhoan
{
    public Guid Id { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string? VaiTro { get; set; }

    public bool IsActive { get; set; }

    public string? TenHienThi { get; set; }






    

    public DateTime? LastModified { get; set; }
}
