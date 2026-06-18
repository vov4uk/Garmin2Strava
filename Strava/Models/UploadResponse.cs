using Newtonsoft.Json;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class UploadResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("activity_id")]
        public long? ActivityId { get; set; }
    }
}
