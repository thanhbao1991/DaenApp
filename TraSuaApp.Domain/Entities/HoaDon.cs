namespace TraSuaApp.Domain.Entities;

public class HoaDon
{
    public Guid Id { get; set; }
    public DateTime NgayTao { get; set; }
    public Guid? IdKhachHang { get; set; }
    public Guid? IdBan { get; set; }
    public Guid? IdTaiKhoan { get; set; }
    public Guid? IdNhomHoaDon { get; set; }
    public decimal TongTien { get; set; }
    public decimal GiamGia { get; set; }
    public decimal ThanhTien { get; set; }

    public KhachHang? KhachHang { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }

    public ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; }
    public ICollection<Payment> Payments { get; set; }
    public ICollection<ChiTietHoaDonTopping> ChiTietToppings { get; set; }
}