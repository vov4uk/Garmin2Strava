using Garmin.Connect.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Garmin
{
    public interface IGarminClient
    {
        Task AuthorizeAsync();
        Task<List<GarminActivity>> GetActivitiesListAsync();
        Task<List<GarminActivity>> GetActivitiesListAsync(DateTime startDate, DateTime endDate);
        Task<bool> DownloadActivityAsync(GarminActivity activity, string localPath);
    }
}
