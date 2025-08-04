using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDon : EntityBase
{

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal ThanhTien { get; set; }


    [NotMapped]
    public int OldId { get; set; }
    public Guid HoaDonId { get; set; }

    public Guid SanPhamBienTheId { get; set; }




    public virtual HoaDon? HoaDon { get; set; }

    public virtual SanPhamBienThe? SanPhamBienThe { get; set; }
    public string TenSanPham { get; set; } = "";
    public string TenBienThe { get; set; } = "";
    public string? ToppingText { get; set; }
    public string? NoteText { get; set; }

}




