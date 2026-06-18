using Newtonsoft.Json;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class AuthTokenRequest : AuthTokenRequestBase
    {
        [JsonProperty("code")]
        public string ClientCode { get; }

        public AuthTokenRequest(StravaConfig auth, string clientCode) : base(auth, "authorization_code")
        {
            ClientCode = clientCode;
        }
    }
}
