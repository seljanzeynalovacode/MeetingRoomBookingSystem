namespace MRBSystem.Domain.Entities;

public class Floor : BaseEntity
{
    public string Name { get; set; } = null!;
    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public int LevelNumber { get; set; }
}
