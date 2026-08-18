using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.CommentDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class CommentService : ICommentService
{
    private readonly IGenericRepository<Comment> _repository;
    private readonly IGenericRepository<Like> _likeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public CommentService(
        IGenericRepository<Comment> repository,
        IGenericRepository<Like> likeRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _repository = repository;
        _likeRepository = likeRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ResponseCommentDto> CreateAsync(CreateCommentDto dto)
    {
        var entity = _mapper.Map<Comment>(dto);
        entity.UserId = _currentUser.UserId;

        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Include(c => c.Replies)
            .FirstAsync(c => c.Id == entity.Id);

        var result = _mapper.Map<ResponseCommentDto>(created);
        result.IsLikedByCurrentUser = false;
        return result;
    }

    public async Task<ResponseCommentDto> UpdateAsync(Guid id, UpdateCommentDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Comment with id {id} not found.");

        if (entity.UserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You can only edit your own comments.");

        entity.Content = dto.Content;
        entity.IsEdited = true;
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Include(c => c.Replies)
            .FirstAsync(c => c.Id == id);

        var result = _mapper.Map<ResponseCommentDto>(updated);
        result.IsLikedByCurrentUser = updated.Likes.Any(l => l.UserId == _currentUser.UserId);
        return result;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException($"Comment with id {id} not found.");
        }
        if (entity.UserId != _currentUser.UserId)
        {
            throw new UnauthorizedAccessException("You can only delete your own comments.");
        }

        await DeleteWithRepliesAsync(id);
    }

    public async Task<DetailedCommentDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Include(c => c.Replies).ThenInclude(r => r.User)
            .Include(c => c.Replies).ThenInclude(r => r.Likes)
            .Include(c => c.Replies).ThenInclude(r => r.Replies)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)
        {
            throw new NotFoundException($"Comment with id {id} not found.");
        }

        var dto = _mapper.Map<DetailedCommentDto>(entity);
        dto.IsLikedByCurrentUser = entity.Likes.Any(l => l.UserId == _currentUser.UserId);

        foreach (var reply in dto.Replies)
        {
            var replyEntity = entity.Replies.First(r => r.Id == reply.Id);
            reply.IsLikedByCurrentUser = replyEntity.Likes.Any(l => l.UserId == _currentUser.UserId);
        }

        return dto;
    }

    public async Task<ICollection<ResponseCommentDto>> GetAllAsync()
    {
        var entities = await _repository.GetQueryable()
            .Where(c => c.ParentCommentId == null)
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Include(c => c.Replies)
            .ToListAsync();

        var dtos = _mapper.Map<ICollection<ResponseCommentDto>>(entities);

        foreach (var dto in dtos)
        {
            var entity = entities.First(e => e.Id == dto.Id);
            dto.IsLikedByCurrentUser = entity.Likes.Any(l => l.UserId == _currentUser.UserId);
        }

        return dtos;
    }

    public async Task LikeAsync(Guid commentId)
    {
        var exists = await _likeRepository.GetQueryable()
            .AnyAsync(l => l.CommentId == commentId && l.UserId == _currentUser.UserId);

        if (exists)
        {
            return;
        }

        await _likeRepository.AddAsync(new Like
        {
            CommentId = commentId,
            UserId = _currentUser.UserId
        });
    }

    public async Task UnlikeAsync(Guid commentId)
    {
        var like = await _likeRepository.GetQueryable()
            .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == _currentUser.UserId);

        if (like is not null)
            await _likeRepository.DeleteAsync(like.Id);
    }

    private async Task DeleteWithRepliesAsync(Guid commentId)
    {
        var childIds = await _repository.GetQueryable()
            .Where(c => c.ParentCommentId == commentId)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var childId in childIds)
        {
            await DeleteWithRepliesAsync(childId);
        }

        await _repository.DeleteAsync(commentId);
    }
}