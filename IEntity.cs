namespace ru.emlsoft.data;

public interface IEntity
{
    int UserId { get; set; }
    DateTime LastUpdated { get; set; }
    bool IsDeleted { get; set; }
}