using Garmin2StravaFinalSync.Strava.Models;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Strava.Abstract
{
    public interface IKeyRepository
    {
        Task<AuthTokenResponse> GetAsync();
        Task SetAsync(AuthTokenResponse auth);
        bool Exists();
    }
}
