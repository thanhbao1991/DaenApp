using System.ComponentModel.DataAnnotations;

namespace TraSuaAppWeb.Models
{

    public class ChiTietHoaDon
    {
        [Key]
        public int IdChiTietHoaDon { get; set; }
        public int IdHoaDon { get; set; }
        public int IdSanPham { get; set; }

        public string? GhiChu { get; set; }
        public required string TenSanPham { get; set; }
        public required bool TichDiem { get; set; }

        public double SoLuong { get; set; }
        public double DonGia { get; set; }
        public double ThanhTien { get; set; }
        public virtual HoaDon? HoaDon { get; set; }
    }

}