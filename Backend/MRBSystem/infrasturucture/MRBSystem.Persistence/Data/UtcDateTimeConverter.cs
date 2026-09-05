using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MRBSystem.Persistence.Data;

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    // Unspecified-kind values are assumed to already be UTC (not converted) — this codebase
    // always produces DateTime.UtcNow-based values, never an arbitrary local/unspecified one.
    public UtcDateTimeConverter() : base(
       v => v.Kind == DateTimeKind.Utc ? v
           : v.Kind == DateTimeKind.Local ? v.ToUniversalTime()
           : DateTime.SpecifyKind(v, DateTimeKind.Utc),
       v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
