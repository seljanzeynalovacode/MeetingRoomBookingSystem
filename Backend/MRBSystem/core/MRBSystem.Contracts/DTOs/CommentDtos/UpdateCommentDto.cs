namespace MRBSystem.Contracts.DTOs.CommentDtos;

public record UpdateCommentDto
{
    public string Content { get; set; } = null!;
}
