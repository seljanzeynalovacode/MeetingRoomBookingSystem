using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Shared.Exceptions;
using MRBSystem.Application.Teamplates;
using MRBSystem.Contracts.DTOs.NotificationDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IGenericRepository<Notification> _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public NotificationService(
        IGenericRepository<Notification> repository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ICollection<ResponseNotificationDto>> GetMyNotificationsAsync()
    {
        var entities = await _repository.GetQueryable()
            .Where(n => n.UserId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseNotificationDto>>(entities);
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Notification with id {id} not found.");

        if (entity.UserId != _currentUser.UserId)
            throw new ForbiddenException("This notification does not belong to you.");

        entity.IsRead = true;
        entity.ReadAt = DateTime.UtcNow;
        await _repository.UpdateAsync(entity);
    }

    public async Task CreateAsync(Reservation reservation, NotificationType type)
    {
        var (title, message) = NotificationTemplates.Build(
            type,
            reservation.ReservedByUser.FirstName,
            reservation.ReservedByUser.LastName,
            reservation.Title,
            reservation.Room.Name,
            reservation.StartDate,
            reservation.EndDate
        );

        var notification = new Notification
        {
            UserId = reservation.ReservedByUserId,
            ReservationId = reservation.Id,
            Title = title,
            Message = message,
            Type = type,
            ScheduledFor = DateTime.UtcNow
        };

        await _repository.AddAsync(notification);
    }
}