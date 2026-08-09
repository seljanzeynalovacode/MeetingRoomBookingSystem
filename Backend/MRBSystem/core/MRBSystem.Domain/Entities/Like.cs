namespace MRBSystem.Domain.Entities;

public class Like : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid CommentId { get; set; }
    public Comment Comment { get; set; } = null!;
}
