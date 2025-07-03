namespace TraSuaApp.Domain.Entities;

public class CongViecNoiBo
{
    public Guid Id { get; set; }
    public DateTime Ngay { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public bool DaHoanThanh { get; set; }
    public Guid IdNguoiTao { get; set; }
    public DateTime ThoiGianTao { get; set; }
    public DateTime? ThoiGianHoanThanh { get; set; }

    public TaiKhoan NguoiTao { get; set; }
}