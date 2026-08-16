using MRBSystem.Domain.Enums;

namespace MRBSystem.Domain.Entities;

public class AdminActivityLog : BaseEntity
{
    public AdminActivityType Action { get; set; }
    public string Description { get; set; } = null!;
    public Guid? PerformedByUserId { get; set; }
    public AppUser? PerformedByUser { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
