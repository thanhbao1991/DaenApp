namespace TraSuaApp.Domain.Entities;

public partial class DiemKhachHang : EntityBase
{

    public int TongDiem { get; set; }

    public Guid KhachHangId { get; set; }

    public virtual KhachHang? KhachHang { get; set; }
}




