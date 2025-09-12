using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TraSuaAppWeb.Models
{
    public class HoaDon
    {
        [Key]
        public int IdHoaDon { get; set; }

        public DateTime NgayHoaDon { get; set; }
        public DateTime? NgayRa { get; set; }
        public DateTime? NgayNo { get; set; }
        public DateTime? NgayBank { get; set; }
        public DateTime? NgayTra { get; set; }
        public DateTime? NgayShip { get; set; }

        public double TongTien { get; set; }
        public double DaThu { get; set; }
        public double ConLai { get; set; }

        [NotMapped]
        public double tien_mat => DaThu - TienBank;
        public double TienBank { get; set; }
        public bool DaThanhToan { get; set; }
        public bool BaoDon { get; set; }

        public double TienNo { get; set; }
        public int IdNhomHoaDon { get; set; }
        public int? IdKhachHang { get; set; }
        public int IdBan { get; set; }

        public required string ThongTinHoaDon { get; set; }
        public string? DiaChiShip { get; set; }
        public string? DienThoaiShip { get; set; }
        public virtual ICollection<ChiTietHoaDon>? ChiTietHoaDon { get; set; }
    }
}