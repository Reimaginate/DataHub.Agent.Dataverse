namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public static class DateTimeExtensions
{
    public static DateTimeOffset ConvertToSpecificTimeZone(this DateTimeOffset originalDateTimeOffset, TimeSpan targetOffset)
    {
        var utcDateTimeOffset = originalDateTimeOffset.ToUniversalTime();
        var newDateTimeOffset = new DateTimeOffset(utcDateTimeOffset.DateTime, targetOffset);
        var adjustedDateTimeOffset = newDateTimeOffset.ToOffset(targetOffset);
        return adjustedDateTimeOffset;
    }
}