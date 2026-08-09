namespace MRBSystem.Domain.Entities;

public class Room : BaseEntity
{
    public string Name { get; set; } = null!;
    public Guid FloorId { get; set; }
    public Floor Floor { get; set; } = null!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}
