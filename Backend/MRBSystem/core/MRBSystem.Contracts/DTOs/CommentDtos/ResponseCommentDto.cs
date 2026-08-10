namespace MRBSystem.Contracts.DTOs.CommentDtos;

public record ResponseCommentDto : BaseDto
{
    public string Content { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public bool IsEdited { get; set; }
    public int LikesCount { get; set; }
    public int RepliesCount { get; set; }
}
