namespace Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;

public interface ITimeService
{
    DateTimeOffset Now();
    DateTimeOffset Today();
    DateTimeOffset Parse(string val);
    DateTimeOffset Parse(DateTime? val);
    DateTimeOffset? ToDataHubTimeZone(DateTimeOffset? val);
}

public class TimeService : ITimeService
{
    TimeZoneInfo _timeZoneInfo;

    public TimeService()
    {

    }

    public TimeService(string timeZoneId)
    {
        if (timeZoneId == null)
        {
            throw new ArgumentException("TIME_ZONE_NULL");
        }

        try
        {
            _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"The time zone '{timeZoneId}' was not found.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException($"The time zone '{timeZoneId}' is invalid.");
        }
        catch (Exception ex)
        {
            throw new Exception("TIME_SERVICE_INIT_FAIL: " + ex.Message);
        }

    }

    public DateTimeOffset Now()
    {
        var now = DateTimeOffset.Now;
        if (_timeZoneInfo != null)
        {
            now = now.ToOffset(_timeZoneInfo.BaseUtcOffset);
        }
        return now;
    }

    public DateTimeOffset Today()
    {
        return Now().Date;
    }

    public DateTimeOffset Parse(string val)
    {
        if (string.IsNullOrEmpty(val)) throw new ArgumentException();
        
        var ret = DateTimeOffset.Parse(val);
        return _timeZoneInfo != null ? ret.ToOffset(_timeZoneInfo.BaseUtcOffset) : ret;
    }

    public DateTimeOffset Parse(DateTime? val)
    {
        if (val == null) throw new ArgumentException();

        var ret = (DateTimeOffset)val;
        return _timeZoneInfo != null ? ret.ToOffset(_timeZoneInfo.BaseUtcOffset) : ret;
    }

    public DateTimeOffset? ToDataHubTimeZone(DateTimeOffset? val)
    {
        if (val == null) return null;
        
        return _timeZoneInfo != null ? val.Value.ToOffset(_timeZoneInfo.BaseUtcOffset) : val.Value;
    }

}