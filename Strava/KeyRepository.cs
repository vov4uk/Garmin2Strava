using Garmin2StravaFinalSync.Strava.Abstract;
using Garmin2StravaFinalSync.Strava.Models;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Strava
{
    public class KeyRepository : IKeyRepository
    {
        private const string fileName = "auth.json";

        public async Task<AuthTokenResponse> GetAsync()
        {
            string json = await File.ReadAllTextAsync(fileName);
            return JsonConvert.DeserializeObject<AuthTokenResponse>(json) ?? throw new InvalidDataException("auth file not in correct format");
        }

        public async Task SetAsync(AuthTokenResponse auth)
        {
            string json = JsonConvert.SerializeObject(auth, Formatting.Indented);
            await File.WriteAllTextAsync(fileName, json);
        }

        public bool Exists()
        {
            return File.Exists(fileName);
        }
    }
}
