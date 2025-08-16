using Strava.Activities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Strava.Abstract
{
    public interface IStravaClient
    {
        Task<List<Activity>> GetActivitiesListAsync(DateTime from, DateTime to);
        Task<long> UploadActivityAsync(string path);

        Task UpdateActivityAsync(long activityId, string name, string description);
    }
}
