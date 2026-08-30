using MRBSystem.Domain.Enums;

namespace MRBSystem.Application.Teamplates;

public static class NotificationTemplates
{
    private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4); // Bakı UTC+4, DST yoxdur

    public static (string Title, string Message) Build(
        NotificationType type,
        string firstName,
        string lastName,
        string meetingTitle,
        string roomName,
        string buildingName,
        DateTime startDate,
        DateTime endDate)
    {
        var localStart = startDate.Add(BakuOffset);
        var localEnd = endDate.Add(BakuOffset);

        var date = localStart.ToString("dd.MM.yyyy");
        var startTime = localStart.ToString("HH:mm");
        var endTime = localEnd.ToString("HH:mm");

        return type switch
        {
            NotificationType.Created => (
                "Rezervasiya yaradıldı",
                $"""
            Hörmətli {firstName} {lastName},
            Meeting room rezervasiyanız uğurla yaradıldı.
            Meeting: {meetingTitle}
            Bina: {buildingName}
            Otaq: {roomName}
            Tarix: {date}
            Saat: {startTime} - {endTime}
            Rezervasiyanız uğurla təsdiqləndi.
            Hörmətlə,
            MeetingSpace
            """
            ),
            NotificationType.Updated => (
                "Rezervasiya yeniləndi",
                $"""
            Hörmətli {firstName} {lastName},
            Meeting room rezervasiyanız yeniləndi.
            Meeting: {meetingTitle}
            Bina: {buildingName}
            Otaq: {roomName}
            Tarix: {date}
            Saat: {startTime} - {endTime}
            Hörmətlə,
            MeetingSpace
            """
            ),
            NotificationType.Cancelled => (
                "Rezervasiya ləğv edildi",
                $"""
            Hörmətli {firstName} {lastName},
            Meeting room rezervasiyanız ləğv edildi.
            Meeting: {meetingTitle}
            Bina: {buildingName}
            Otaq: {roomName}
            Tarix: {date}
            Saat: {startTime} - {endTime}
            Hörmətlə,
            MeetingSpace
            """
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    }

    public static (string Title, string Message) BuildGuestInvitation(
    string? guestName,
    string meetingTitle,
    string roomName,
    string buildingName,
    DateTime startDate,
    DateTime endDate)
    {
        var localStart = startDate.Add(BakuOffset);
        var localEnd = endDate.Add(BakuOffset);

        var date = localStart.ToString("dd.MM.yyyy");
        var startTime = localStart.ToString("HH:mm");
        var endTime = localEnd.ToString("HH:mm");
        var greeting = string.IsNullOrWhiteSpace(guestName) ? "Salam!" : $"Salam, {guestName}!";

        return (
            "İclasa dəvət olundunuz",
            $"""
        {greeting}

        Sizi aşağıdakı görüşə dəvət edirik.

        Görüş: {meetingTitle}
        Bina: {buildingName}
        Otaq: {roomName}
        Tarix: {date}
        Saat: {startTime} – {endTime}

        Görüşdə iştirakınızı gözləyirik.

        Hörmətlə,
        MeetingSpace
        """
        );
    }
}
