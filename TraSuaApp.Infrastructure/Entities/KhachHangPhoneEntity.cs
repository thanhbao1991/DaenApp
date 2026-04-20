namespace TraSuaApp.Infrastructure.Entities;

public partial class KhachHangPhone
{
    public Guid Id { get; set; }

    public string SoDienThoai { get; set; } = null!;

    public bool IsDefault { get; set; }

    public Guid KhachHangId { get; set; }





    

    public DateTime? LastModified { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
