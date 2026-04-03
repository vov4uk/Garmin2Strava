using System.Text.Json.Serialization;

namespace Garmin2StravaFinalSync.Strava.Models
{
    public class AuthTokenRequest : AuthTokenRequestBase
    {
        [JsonPropertyName("code")]
        public string ClientCode { get; }

        public AuthTokenRequest(StravaConfig auth, string clientCode) : base(auth, "authorization_code")
        {
            ClientCode = clientCode;
        }
    }
}
