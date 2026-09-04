namespace MRBSystem.Contracts.Services;

public interface IRoomCalendarBroadcastService
{
    Task NotifyRoomCalendarChangedAsync(Guid roomId, Guid? reservedByUserId = null);
}
