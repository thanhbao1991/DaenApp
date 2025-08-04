using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public abstract class DtoBase
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual string Ten { get; set; } = string.Empty;
    public int? Stt { get; set; }

    // ✅ Route API dùng cho BaseDataProvider, BaseEditForm...
    public abstract string ApiRoute { get; }

    // ✅ Chuỗi tìm kiếm không dấu + tên viết tắt (mặc định: Ten + STT)
    public virtual string TimKiem =>
        TextSearchHelper.NormalizeText($"{Ten}") + " " +
        TextSearchHelper.GetShortName(Ten ?? "");
}
