using System.Text.Json.Serialization;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public abstract class AuthTokenRequestBase
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; }

        [JsonPropertyName("grant_type")]
        public string GrantType { get; }

        protected AuthTokenRequestBase(StravaConfig auth, string grant)
        {
            ClientId = auth.ClientId;
            ClientSecret = auth.ClientSecret;
            GrantType = grant;
        }
    }
}
