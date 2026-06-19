using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sac.Helpers;

public static class RealUTC
{

    /// <summary>
    /// Return the real UTC offset string for the specified timezone, taking into account DST if active.
    /// </summary>
    /// <param name="tz"></param>
    /// <returns></returns>
    public static string Get(TimeZoneInfo tz)
    {
        // Use DateTimeOffset.UtcNow so the TimeZoneInfo offset is computed for the correct instant
        // including daylight saving rules for all hemispheres.
        // Btw, (DateTime.UtcNow) - caused troubles in southern hemisphere during their summer time (DST)
     
        TimeSpan effectiveOffset = tz.GetUtcOffset(DateTimeOffset.UtcNow);
        string sign = effectiveOffset >= TimeSpan.Zero ? "+" : "-";
        string offsetStr = $"{sign}{effectiveOffset.Duration():hh\\:mm}";

        return $"(UTC{offsetStr})";
    }
}
