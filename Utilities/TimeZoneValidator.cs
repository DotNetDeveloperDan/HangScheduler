using TimeZoneConverter;

namespace HangScheduler.Api.Utilities;

public static class TimeZoneValidator
{
    public static bool IsValidTimeZone(string timeZoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException ex)
        {
            try
            {
                var windowsTimeZone = TZConvert.IanaToWindows(timeZoneId); // Throws exception if invalid
                TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}