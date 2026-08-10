namespace MRBSystem.Domain.Entities;

public class RoomEquipment : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public int Quantity { get; set; } = 1; // bir otagda nece eyni avadanliq oldugunu gosterir
}
