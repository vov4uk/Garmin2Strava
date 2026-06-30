using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync.Garmin
{
    public class GarminClient: IGarminClient
    {
        GarminConnectClient client = null;
        private readonly ILogger<GarminClient> _logger;
        private readonly GarminConfig _config;

        public GarminClient(GarminConfig options, ILogger<GarminClient> logger)
        {
            _config = options;
            _logger = logger;
        }

        public Task AuthorizeAsync()
        {
            BasicAuthParameters authParameters = new(_config.GarminLogin, _config.GarminPassword);
            HttpClient httpClient = new();
            client = new(new GarminConnectContext(httpClient, authParameters));
            return Task.CompletedTask;
        }

        public async Task<List<GarminActivity>> GetActivitiesListAsync()
        {
            _logger.LogInformation("Reading Garmin activities, please wait...");

            GarminActivity[] garminActivities = await client.GetActivities(0, 20, CancellationToken.None);

            if (garminActivities.Count() == 0)
            {
                _logger.LogWarning($"No Garmin activities");
            }
            return garminActivities.ToList();
        }
        public async Task<List<GarminActivity>> GetActivitiesListAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Reading Garmin activities, please wait...");

            GarminActivity[] garminActivities = await client.GetActivitiesByDate(startDate, endDate, null, CancellationToken.None);

            if (garminActivities.Count() == 0)
            {
                _logger.LogWarning($"No Garmin activities");
            }
            return garminActivities.ToList();
        }

        public async Task<bool> DownloadActivityAsync(GarminActivity activity, string localPath)
        {
            try
            {


                activity.CreateYearDirectories(localPath);

                if (!activity.Exists_Fit(localPath))
                {
                    var array = await client.DownloadActivity(activity.ActivityId, ActivityDownloadFormat.ORIGINAL);
                    using var z = new ZipArchive(new MemoryStream(array), ZipArchiveMode.Read);
                    z.ExtractToDirectory(localPath, true);

                    if (activity.OriginalExists_Fit(localPath))
                    {
                        File.Move(activity.OriginalFullPath_Fit(localPath), activity.FullPath_Fit(localPath), true);
                    }

                    if (!activity.Exists_Gpx(localPath))
                    {
                        _logger.LogInformation($"Downloading GPX for {activity.Name_Gpx()}");
                        var gpx = await client.DownloadActivity(activity.ActivityId, ActivityDownloadFormat.GPX);
                        await File.WriteAllBytesAsync(activity.FullPath_Gpx(localPath), gpx);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }

        }
    }

    public static class GarminExtensions
    {
        public static void CreateYearDirectories(this GarminActivity activity, string localPath)
        {
            var year = activity.StartTimeLocal.Year.ToString();
            if (!Directory.Exists(Path.Combine(localPath, year)))
            {
                Directory.CreateDirectory(Path.Combine(localPath, year));
            }
            if (!Directory.Exists(Path.Combine(localPath, "gpx", year)))
            {
                Directory.CreateDirectory(Path.Combine(localPath, "gpx", year));
            }
        }

        public static bool Exists_Gpx(this GarminActivity activity, string localPath)
        {
            return File.Exists(activity.FullPath_Gpx(localPath));
        }

        public static bool Exists_Fit(this GarminActivity activity, string localPath)
        {
            return File.Exists(activity.FullPath_Fit(localPath));
        }

        public static bool OriginalExists_Fit(this GarminActivity activity, string localPath)
        {
            return File.Exists(activity.OriginalFullPath_Fit(localPath));
        }

        public static bool OriginalExists_Gpx(this GarminActivity activity, string localPath)
        {
            return File.Exists(activity.OriginalFullPath_Gpx(localPath));
        }

        public static string OriginalFullPath_Fit(this GarminActivity activity, string localPath)
        {
            return Path.Combine(localPath, activity.OriginalName_Fit());
        }

        public static string OriginalFullPath_Gpx(this GarminActivity activity, string localPath)
        {
            return Path.Combine(localPath, activity.OriginalName_Gpx());
        }

        public static string FullPath_Fit(this GarminActivity activity, string localPath)
        {
            return Path.Combine(localPath, activity.StartTimeLocal.Year.ToString(), activity.Name_Fit());
        }

        public static string FullPath_Gpx(this GarminActivity activity, string localPath)
        {
            return Path.Combine(localPath, activity.StartTimeLocal.Year.ToString(), activity.Name_Gpx());
        }

        public static string Name_Fit(this GarminActivity activity)
        {
            return $"{activity.Name()}.fit";
        }

        public static string Name_Gpx(this GarminActivity activity)
        {
            return $"{activity.Name()}.gpx";
        }

        public static string OriginalName_Fit(this GarminActivity activity)
        {
            return $"{activity.OriginalName()}.fit";
        }
        public static string OriginalName_Gpx(this GarminActivity activity)
        {
            return $"{activity.OriginalName()}.gpx";
        }


        private static string OriginalName(this GarminActivity activity)
        {
            return $"{activity.ActivityId}_ACTIVITY";
        }

        private static string Name(this GarminActivity activity)
        {
            return $"{activity.StartTimeLocal:yyyyMMdd_HHmm}_{activity.ActivityType.TypeKey}_{activity.ActivityId}";
        }
    }
}
