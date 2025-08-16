using Garmin.Connect.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Garmin
{
    public interface IGarminClient
    {
        Task AuthorizeAsync();
        Task<List<GarminActivity>> GetActivitiesListAsync(DateTime from, DateTime to);
        Task DownloadActivityAsync(long activityId, string localPath);
    }
}
