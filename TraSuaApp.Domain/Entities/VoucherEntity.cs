namespace TraSuaApp.Domain.Entities;

public partial class Voucher
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public decimal GiaTri { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public bool DangSuDung { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public decimal? DieuKienToiThieu { get; set; }

    public bool IsDeleted { get; set; }

    public string KieuGiam { get; set; } = null!;

    public DateTime? LastModified { get; set; }

    public int? SoLanSuDungToiDa { get; set; }

    public int OldId { get; set; }

    public virtual ICollection<ChiTietHoaDonVoucher> ChiTietHoaDonVouchers { get; set; } = new List<ChiTietHoaDonVoucher>();
}
