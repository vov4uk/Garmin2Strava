using Newtonsoft.Json;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public abstract class AuthTokenRequestBase
    {
        [JsonProperty("client_id")]
        public string ClientId { get; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; }

        [JsonProperty("grant_type")]
        public string GrantType { get; }

        protected AuthTokenRequestBase(StravaConfig auth, string grant)
        {
            ClientId = auth.ClientId;
            ClientSecret = auth.ClientSecret;
            GrantType = grant;
        }
    }
}
