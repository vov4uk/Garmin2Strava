/*
 * Runs on MS Windows only
 * Built by Visual Studio 2022 Community
 * Target framework = .NET 6.0
 *    Nullable = Disable
 *    Implicit global usings = false
 * Installed Packages:
 *    Unofficial.Garmin.Connect 0.2.0+
 *    Microsoft.Extensions.Configuration
 *    Microsoft.Extensions.Configuration.Json
 *    Microsoft.Extensions.Configuration.Binder
 *    Newtonsoft.Json
 */

using System;

internal class Settings
{
    /// <summary>
    /// Update activities that have taken place after a certain date.
    /// If this or the following property is missing in the configuration file today is used.
    /// </summary>
    public DateTime DateAfter { get; set; }

    /// <summary>
    /// Update activities that have taken place before a certain date.
    /// If this or the previous property is missing in the configuration file tomorrow is used.
    /// </summary>
    public DateTime DateBefore { get; set; }

    /// <summary>
    /// Garmin account login email
    /// </summary>
    public string GarminLogin { get; set; }

    /// <summary>
    /// Garmin account password
    /// </summary>
    public string GarminPassword { get; set; }

    /// <summary>
    /// true to append used gears to the description. Requires UpdateName = true.
    /// </summary>
    public bool GearsToDescription { get; set; }

    /// <summary>
    /// true to append specified Garmin activity properties to the Strava activity description. Requires UpdateName = true.
    /// The value of this configuration item is the list of properties separated by semicolons. Optional formatting string follows the colon after the property name.
    /// Example: "VO2MaxValue;MaxHr;AvgStrideLength:0.0"
    /// List of Garmin activity properties: https://github.com/sealbro/dotnet.garmin.connect/blob/main/Garmin.Connect/Models/GarminActivity.cs
    /// Custom numeric format strings: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings
    /// </summary>
    public string PropertiesToDescription { get; set; }

    /// <summary>
    /// Strava Client ID obtained by registering your API application at https://www.strava.com/settings/api
    /// </summary>
    public int StravaClientId { get; set; }
    /// <summary>
    /// Strava Client Secret obtained by registering your API application at https://www.strava.com/settings/api
    /// </summary>
    public string StravaSecret { get; set; }
    public string StravaCode { get; set; }
    public string StravaAccessToken { get; set; }
    public string StravaRefreshToken { get; set; }
    public long StravaTokenExpire { get; set; }
    /// <summary>
    /// true to update Strava activity description when the Garmin activity description is not empty and different
    /// if true than Strava activity description is also updated when <see cref="PropertiesToDescription"/> or <see cref="GearsToDescription"/> are set.
    /// </summary>
    public bool UpdateDescription { get; set; }

    /// <summary>
    /// true to update Strava activity name when the Garmin activity name is different
    /// </summary>
    public bool UpdateName { get; set; }
    /// <summary>
    /// true to update Strava athlete weight from Garmin.
    /// </summary>
    public bool UpdateWeight { get; set; }

    public bool IsTokenValid()
    {
        var date = new DateTime(1970, 1, 1);
        return DateTime.Now < date.AddSeconds(StravaTokenExpire);
    }
}
