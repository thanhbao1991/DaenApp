using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public partial class Voucher : EntityBase
{
    [NotMapped]
    public int OldId { get; set; }
    public string Ten { get; set; } = null!;


    public string? KieuGiam { get; set; }

    public decimal GiaTri { get; set; }

    public decimal? DieuKienToiThieu { get; set; }  // 🟟 THÊM: Điều kiện hóa đơn tối thiểu

    public int? SoLanSuDungToiDa { get; set; }      // 🟟 TUỲ CHỌN: giới hạn số lượt dùng

    public DateTime NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public bool DangSuDung { get; set; }

    public virtual ICollection<ChiTietHoaDonVoucher> ChiTietHoaDonVouchers { get; set; } = new List<ChiTietHoaDonVoucher>();
}




