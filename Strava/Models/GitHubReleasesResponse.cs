using System;
using Newtonsoft.Json;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class GitHubReleasesResponse
    {
        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonProperty("name")]
        public Version Version { get; set; } = new Version();
    }
}
