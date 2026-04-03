using Garmin2StravaFinalSync.Strava.Abstract;
using Garmin2StravaFinalSync.Strava.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Strava
{
    public class KeyRepository : IKeyRepository
    {
        private const string fileName = "auth.json";

        public async Task<AuthTokenResponse> GetAsync()
        {
            using FileStream stream = File.OpenRead(fileName);
            return await JsonSerializer.DeserializeAsync<AuthTokenResponse>(stream) ?? throw new EndOfStreamException("auth file not in correct format");
        }

        public async Task SetAsync(AuthTokenResponse auth)
        {
            using FileStream stream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(stream, auth);
            await stream.DisposeAsync();
        }

        public bool Exists()
        {
            return File.Exists(fileName);
        }
    }
}
