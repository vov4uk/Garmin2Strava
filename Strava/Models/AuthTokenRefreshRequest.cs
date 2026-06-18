using Newtonsoft.Json;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class AuthTokenRefreshRequest : AuthTokenRequestBase
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; }

        public AuthTokenRefreshRequest(StravaConfig auth, string token) : base(auth, "refresh_token")
        {
            RefreshToken = token;
        }
    }
}
