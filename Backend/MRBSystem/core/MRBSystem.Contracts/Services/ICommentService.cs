using MRBSystem.Contracts.DTOs.CommentDtos;

namespace MRBSystem.Contracts.Services;

public interface ICommentService
{
    Task<ResponseCommentDto> CreateAsync(CreateCommentDto dto);
    Task<ResponseCommentDto> UpdateAsync(Guid id, UpdateCommentDto dto);
    Task DeleteAsync(Guid id);
    Task<DetailedCommentDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseCommentDto>> GetAllAsync();
    Task LikeAsync(Guid commentId);
    Task UnlikeAsync(Guid commentId);
}
