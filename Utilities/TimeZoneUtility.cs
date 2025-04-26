namespace HangScheduler.Api.Utilities
{
    public static class TimeZoneUtility
    {
        public static DateTimeOffset ConvertToDateTimeOffset(DateTime dateTime, TimeZoneInfo timeZone)
        {
            // If the input DateTime is UTC, convert to the time zone's time
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZone);
            }
            // If the input DateTime is local, adjust it to the target time zone
            else if (dateTime.Kind == DateTimeKind.Local)
            {
                dateTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, timeZone);
            }

            // Get the offset for the target time zone
            TimeSpan offset = timeZone.GetUtcOffset(dateTime);

            // Create and return the DateTimeOffset
            return new DateTimeOffset(dateTime, offset);
        }
    }
}
