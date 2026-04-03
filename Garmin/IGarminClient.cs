using Garmin.Connect.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Garmin
{
    public interface IGarminClient
    {
        Task AuthorizeAsync();
        Task<List<GarminActivity>> GetActivitiesListAsync();
        Task<bool> DownloadActivityAsync(long activityId, string localPath);
    }
}
