namespace TraSuaApp.Domain.Entities;

public partial class KhachHangPhone : EntityBase
{


    public string SoDienThoai { get; set; } = null!;

    public bool IsDefault { get; set; }

    public Guid KhachHangId { get; set; }

    public virtual KhachHang? KhachHang { get; set; }
}




