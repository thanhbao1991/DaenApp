namespace TraSuaApp.Domain.Entities;

public partial class KhachHangAddress : EntityBase
{

    public string DiaChi { get; set; } = null!;

    public bool IsDefault { get; set; }

    public Guid KhachHangId { get; set; }

    public virtual KhachHang? KhachHang { get; set; }
}




