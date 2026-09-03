using Microsoft.Extensions.Configuration;
using MRBSystem.Contracts.Services;

namespace MRBSystem.Application.Services;

public class TimeZoneService : ITimeZoneService
{
    private readonly TimeZoneInfo _timeZone;

    public TimeZoneService(IConfiguration configuration)
    {
        var timeZoneId = configuration["Localization:TimeZoneId"]
            ?? throw new InvalidOperationException("Localization:TimeZoneId appsettings.json-da teyin olunmayıb.");

        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    public DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _timeZone);

    public DateTime ToUtc(DateTime local)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _timeZone);
}
