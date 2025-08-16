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

        public async Task<List<GarminActivity>> GetActivitiesListAsync(DateTime from, DateTime to)
        {
            _logger.LogInformation("Reading Garmin activities, please wait...");
            GarminActivity[] garminActivities = await client.GetActivitiesByDate(from, to, null);
            if (garminActivities.Length == 0)
            {
                _logger.LogWarning($"No Garmin activities");
            }
            return garminActivities.ToList();
        }

        public async Task DownloadActivityAsync(long activityId, string localPath)
        {
            try
            {
                if (!File.Exists(Path.Combine(localPath, $"{activityId}_ACTIVITY.fit" )))
                {
                    var array = await client.DownloadActivity(activityId, ActivityDownloadFormat.ORIGINAL);
                    ZipArchive z = new ZipArchive(new MemoryStream(array), ZipArchiveMode.Read);
                    z.ExtractToDirectory(localPath);
                    _logger.LogInformation($"Garmin activity {activityId} downloaded");
                }
                else
                {
                    _logger.LogInformation($"Garmin activity {activityId} already downloaded. Skip!");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

        }
    }
}
