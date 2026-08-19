using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.CommentDtos;

public record CreateCommentDto
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = null!;

    public Guid? ParentCommentId { get; set; }
}