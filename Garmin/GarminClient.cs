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
            if (garminActivities.Length == 0)
            {
                _logger.LogWarning($"No Garmin activities");
            }
            return garminActivities.ToList();
        }

        public async Task<bool> DownloadActivityAsync(long activityId, string localPath)
        {
            try
            {
                if (!File.Exists(Path.Combine(localPath, $"{activityId}_ACTIVITY.fit" )))
                {
                    var array = await client.DownloadActivity(activityId, ActivityDownloadFormat.ORIGINAL);
                    ZipArchive z = new ZipArchive(new MemoryStream(array), ZipArchiveMode.Read);
                    z.ExtractToDirectory(localPath);

                    if (!File.Exists(Path.Combine(localPath,"gpx", $"{activityId}_ACTIVITY.gpx")))
                    {
                        var gpx = await client.DownloadActivity(activityId, ActivityDownloadFormat.GPX);
                        await File.WriteAllBytesAsync(Path.Combine(localPath, "gpx", $"{activityId}_ACTIVITY.gpx"), gpx);
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
}
