namespace TraSuaApp.Infrastructure.Entities;

public partial class KhachHang
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;


    public int ThuTu { get; set; }

    public bool DuocNhanVoucher { get; set; }

    

    public DateTime? LastModified { get; set; }



    


    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<KhachHangAddress> KhachHangAddresses { get; set; } = new List<KhachHangAddress>();

    public virtual ICollection<KhachHangPhone> KhachHangPhones { get; set; } = new List<KhachHangPhone>();

    public virtual ICollection<KhachHangGiaBan> KhachHangGiaBans { get; set; } = new List<KhachHangGiaBan>();
    public string? FavoriteMon { get; set; }
    public string TimKiem { get; set; }
}
