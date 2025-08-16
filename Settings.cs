using Garmin2StravaFinalSync.Garmin;
using Garmin2StravaFinalSync.Strava.Models;
using System;

public class Settings
{
    public DateTime MinActivityDate { get; set; } = new DateTime(2025, 08, 01);

    public int PeriodDays { get; set; } = 30;

    public bool UpdateName { get; set; } = true;

    public GarminConfig Garmin {  get; set; }

    public StravaConfig Strava {  get; set; }

}
