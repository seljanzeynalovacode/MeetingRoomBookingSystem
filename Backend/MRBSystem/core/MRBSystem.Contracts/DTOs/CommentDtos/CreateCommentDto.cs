namespace MRBSystem.Contracts.DTOs.CommentDtos;

public record CreateCommentDto
{
    public string Content { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
}
