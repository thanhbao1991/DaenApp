namespace TraSuaApp.Shared.Dtos;

public class CustomerPhoneNumberDto
{
    public Guid Id { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}