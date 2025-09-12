using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TraSuaAppWeb.Models
{
    public class SanPham
    {
        [Key]
        public int IdSanPham { get; set; }


        public double DonGia { get; set; }
        public required string TenSanPham { get; set; }
        public bool NgungBan { get; set; }
        public bool TichDiem { get; set; }

        public bool? ChiTieu { get; set; } //baán mua vieêệc lamàm
        public required string TimKiem { get; set; }


    }
}