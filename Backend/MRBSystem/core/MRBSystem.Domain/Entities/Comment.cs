namespace MRBSystem.Domain.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = null!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public bool IsEdited { get; set; } = false;
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
