using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Garmin2StravaFinalSync
{
    internal class GarminClient
    {
        private readonly string _login;
        private readonly string _password;
        GarminConnectClient client = null;

        public GarminClient(string login, string password)
        {
            _login = login;
            _password = password;
        }

        public Task AuthorizeAsync()
        {
            BasicAuthParameters authParameters = new(_login, _password);
            HttpClient httpClient = new();
            client = new(new GarminConnectContext(httpClient, authParameters));
            return Task.CompletedTask;
        }

        public async Task<List<GarminActivity>> GetActivitiesListAsync(DateTime DateAfter, DateTime DateBefore)
        {
            Console.WriteLine("Reading Garmin activities, please wait...");
            GarminActivity[] garminActivities = await client.GetActivitiesByDate(DateAfter, DateBefore.AddDays(-1), null);
            if (garminActivities.Length == 0)
            {
                Console.Write($"No Garmin activities");
            }
            return garminActivities.ToList();
        }

        public async Task DownloadActivityAsync(long activityId, string localPath)
        {
            try
            {
                var array = await client.DownloadActivity(activityId, ActivityDownloadFormat.ORIGINAL);
                ZipArchive z = new ZipArchive(new MemoryStream(array), ZipArchiveMode.Read);
                z.ExtractToDirectory(Path.Combine(localPath, "fit"));
                Console.WriteLine($"\t! Garmin activity {activityId} downloaded");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }
    }
}
