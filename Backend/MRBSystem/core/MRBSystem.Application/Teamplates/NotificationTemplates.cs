using MRBSystem.Domain.Enums;

namespace MRBSystem.Application.Teamplates;

public static class NotificationTemplates
{
    public static (string Title, string Message) Build(
        NotificationType type,
        string firstName,
        string lastName,
        string meetingTitle,
        string roomName,
        DateTime startDate,
        DateTime endDate)
    {
        var date = startDate.ToString("dd.MM.yyyy");
        var startTime = startDate.ToString("HH:mm");
        var endTime = endDate.ToString("HH:mm");

        return type switch
        {
            NotificationType.Created => (
                "Rezervasiya yaradıldı",
                $"""
            Hörmətli {firstName} {lastName},
            Meeting room rezervasiyanız uğurla yaradıldı.
            Meeting: {meetingTitle}
            Otaq: {roomName}
            Tarix: {date}
            Saat: {startTime} - {endTime}
            Rezervasiyanız uğurla təsdiqləndi.
            Hörmətlə,
            MRBSystem
            """
            ),
            NotificationType.Updated => (
                "Rezervasiya yeniləndi",
                $"""
            Hörmətli {firstName} {lastName},
            Meeting room rezervasiyanız yeniləndi.
            Meeting: {meetingTitle}
            Otaq: {roomName}
            Tarix: {date}
            Saat: {startTime} - {endTime}
            Hörmətlə,
            MRBSystem
            """
            ),
            NotificationType.Cancelled => (
                "Rezervasiya ləğv edildi",
                $"""
            Hörmətli {firstName} {lastName},
            Meeting room rezervasiyanız ləğv edildi.
            Meeting: {meetingTitle}
            Otaq: {roomName}
            Tarix: {date}
            Saat: {startTime} - {endTime}
            Hörmətlə,
            MRBSystem
            """
            ),
           
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}