using TraSuaApp.Infrastructure.Dtos;

public class KhachHangAddressDto : DtoBase
{




    public Guid KhachHangId { get; set; }
    public string DiaChi { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
