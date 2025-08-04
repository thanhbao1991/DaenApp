namespace TraSuaApp.Shared.Interfaces;

public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime LastModified { get; set; }
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
