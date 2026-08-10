namespace MRBSystem.Contracts.DTOs.CommentDtos;

public record DetailedCommentDto : BaseDto
{
    public string Content { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public bool IsEdited { get; set; }
    public ICollection<ResponseCommentDto> Replies { get; set; } = new List<ResponseCommentDto>();
    public ICollection<Guid> LikedByUserIds { get; set; } = new List<Guid>();
}
