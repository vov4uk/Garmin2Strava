using System;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class StravaConfig
    {
        public string CallBackUrl { get; set; } = string.Empty;
        public string[] Scope { get; set; } = Array.Empty<string>();
        public string ResponseType { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}
