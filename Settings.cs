using Garmin2StravaFinalSync.Garmin;
using Garmin2StravaFinalSync.Strava.Models;

public class Settings
{
    public bool UpdateName { get; set; } = true;

    public GarminConfig Garmin {  get; set; }

    public StravaConfig Strava {  get; set; }

}
