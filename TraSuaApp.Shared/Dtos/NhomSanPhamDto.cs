using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.Shared.Dtos;

public class NhomSanPhamDto : IHasId, IHasRoute
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int STT { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string ApiRoute => "nhomsanpham";
}