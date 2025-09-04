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

    public abstract string ApiRoute { get; }

    public virtual string TimKiem =>
        $"{Ten.ToLower()} " +
        TextSearchHelper.NormalizeText($"{Ten}") + " " +
        TextSearchHelper.NormalizeText($"{Ten.Replace(" ", "")}") + " " +
        TextSearchHelper.GetShortName(Ten ?? "");
}
