using Microsoft.AspNetCore.SignalR;
using MRBSystem.Contracts.Services;
using MRBSystem.WebAPI.Hubs;

namespace MRBSystem.WebAPI.Services;

public class SignalRRoomCalendarBroadcastService : IRoomCalendarBroadcastService
{
    private readonly IHubContext<RoomCalendarHub> _hubContext;

    public SignalRRoomCalendarBroadcastService(IHubContext<RoomCalendarHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyRoomCalendarChangedAsync(Guid roomId, Guid? reservedByUserId = null)
    {
        await _hubContext.Clients.Group(roomId.ToString()).SendAsync("RoomCalendarChanged");
        await _hubContext.Clients.Group("Admins").SendAsync("ReservationsChanged");

        if (reservedByUserId.HasValue)
        {
            await _hubContext.Clients.Group($"user:{reservedByUserId}").SendAsync("MyReservationsChanged");
        }
    }
}
