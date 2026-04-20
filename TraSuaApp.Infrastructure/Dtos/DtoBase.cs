namespace TraSuaApp.Infrastructure.Dtos;

public abstract class DtoBase
{
    public Guid Id { get; set; }
    public DateTime? LastModified { get; set; }
    public virtual string ApiRoute => "";
}
