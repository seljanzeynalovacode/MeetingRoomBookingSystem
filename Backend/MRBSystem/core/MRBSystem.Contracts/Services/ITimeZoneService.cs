namespace MRBSystem.Contracts.Services;

public interface ITimeZoneService
{
    DateTime ToLocal(DateTime utc);
    DateTime ToUtc(DateTime local);
}
