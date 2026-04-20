using TraSuaApp.Infrastructure.Dtos;

public class KhachHangPhoneDto : DtoBase
{



    public Guid KhachHangId { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
