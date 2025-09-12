using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TraSuaAppWeb.Models
{
    public class KhachHang
    {
        [Key]
        public int IdKhachHang { get; set; }
        public required string DienThoai { get; set; }
        public required string DiaChi { get; set; }
        public required string TenKhachHang { get; set; }
        public required string TimKiem { get; set; }

    }
}