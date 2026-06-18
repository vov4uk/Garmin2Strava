using System;
using Newtonsoft.Json;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class AuthTokenResponse
    {
        [JsonProperty("token_type")]
        public string TokenType{ get; set; } = string.Empty;

        [JsonProperty("expires_at")]
        public long ExpiresAt { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        public bool IsExpired
        {
            get
            {
                return ExpiresAt < DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }
    }
}
