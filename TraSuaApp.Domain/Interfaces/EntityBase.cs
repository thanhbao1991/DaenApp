namespace TraSuaApp.Domain.Entities;

public abstract class EntityBase
{
    //public int? Stt { get; set; }

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
