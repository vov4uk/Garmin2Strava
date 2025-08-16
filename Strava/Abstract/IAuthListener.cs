using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Strava.Abstract
{
    public interface IAuthListener
    {
        Task<string> GetAuthCodeAsync();
    }
}
