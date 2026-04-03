using System;
using System.Text.Json.Serialization;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class GitHubReleasesResponse
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public Version Version { get; set; } = new Version();
    }
}
