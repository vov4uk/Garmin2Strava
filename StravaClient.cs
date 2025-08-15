using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using Activity = Strava.Activities.Activity;


namespace Garmin2StravaFinalSync
{
    internal class StravaClient
    {
        string[] stravaApiUsages = null, stravaApiLimits = null;
        private readonly int _clientId;
        private readonly string _clientSecret;
        private string clientCode;
        internal string clientToken;
        Settings _settings;
        public StravaClient(Settings settings)
        {
            _settings = settings;
            _clientId = settings.StravaClientId;
            _clientSecret = settings.StravaSecret;
            clientCode = settings.StravaCode;
        }

        public async Task AuthorizeAsync()
        {
            var tokenIsValid = _settings.IsTokenValid();
            if (tokenIsValid)
            {
                clientToken = _settings.StravaAccessToken;
            }
            else
            {
                clientToken = await RefreshAccessToken(_settings.StravaRefreshToken);

                if (string.IsNullOrEmpty(clientToken))
                {
                    clientCode = AuthrizeToStrava();
                    clientToken = await GetAccessToken(clientCode);
                    if (string.IsNullOrEmpty(clientToken))
                    {
                        throw new Exception("Unbable to connect to Strava");
                    }
                }
            }

            //if (string.IsNullOrEmpty(clientCode))
            //{
            //    Console.WriteLine("Code is empty");
            //    clientCode = AuthrizeToStrava();
            //}

            //clientToken = await GetAccessToken(clientCode);
            //if (string.IsNullOrEmpty(clientToken))
            //{
            //    clientCode = AuthrizeToStrava();
            //    clientToken = await GetAccessToken(clientCode);
            //    if (string.IsNullOrEmpty(clientToken))
            //    {
            //        throw new Exception("Unbable to connect to Strava");
            //    }
            //}
        }

        public async Task<List<Activity>> GetActivitiesListAsync(DateTime DateAfter, DateTime DateBefore)
        {

            HttpResponseMessage stravaResponse = null;
            using HttpClient stravaHttpClient = new()
            {
                BaseAddress = new Uri("https://www.strava.com")
            };

            WriteLine("Reading Strava activities, please wait...");
            List<Activity> stravaActivities = new();
            for (int stravaActivitiesPage = 1; ; stravaActivitiesPage++)
            {
                string getActivitiesUrl = "/api/v3/athlete/activities?" +
                  $"before={DateBefore.DateTimeToUnixTimestamp()}&" +
                  $"after={DateAfter.DateTimeToUnixTimestamp()}&" +
                  $"page={stravaActivitiesPage}&" +
                  "per_page=200&" +
                  $"access_token={clientToken}";

                stravaResponse = await stravaHttpClient.GetAsync(getActivitiesUrl);
                if (stravaResponse.StatusCode != HttpStatusCode.OK)
                    throw new($"! Error {stravaResponse.StatusCode} when reading Strava activities.");

                checkStravaApiLimits(stravaResponse);

                var activitiesResponse = await stravaResponse.Content.ReadAsStringAsync();

                List<Activity> newActivities = JsonConvert.DeserializeObject<List<Activity>>(activitiesResponse);
                if (!newActivities.Any())
                    break;

                foreach (Activity stravaActivity in newActivities)
                    WriteLine(
                      $"\t{stravaActivity.Type}\t" +
                      $"{stravaActivity.StartDateLocal}\t" +
                      $"{stravaActivity.Name}");

                stravaActivities.AddRange(newActivities);
            }

            if (stravaActivities.Count == 0)
            {
                WriteLine($"No Strava activities");
            }
            return stravaActivities;
        }

        private async Task<string> GetAccessToken(string code)
        {

            try
            {
                using HttpClient stravaHttpClient = new()
                {
                    BaseAddress = new Uri("https://www.strava.com")
                };

//Example cURL Request

//curl - X POST https://www.strava.com/api/v3/oauth/token \
//  -d client_id = ReplaceWithClientID \
//  -d client_secret = ReplaceWithClientSecret \
//  -d code = ReplaceWithCode \
//  -d grant_type = authorization_code


                string authUrl = "/oauth/token?" +
                  $"client_id={_clientId}&" +
                  $"client_secret={_clientSecret}&" +
                  $"code={clientCode}&" +
                  "grant_type=authorization_code";
                HttpResponseMessage stravaResponse = await stravaHttpClient.PostAsync(authUrl, null);
                if (stravaResponse.IsSuccessStatusCode)
                {
                    dynamic stravaAccessResponse = (await stravaResponse.Content.ReadAsStringAsync()).JsonDeserialize();
                    string stravaAccessToken = stravaAccessResponse.access_token;
                    string stravaRefreshToken = stravaAccessResponse.refresh_token;
                    long stravaTokenExpire = stravaAccessResponse.expires_at;

                    SettingsHelpers.AddOrUpdateAppSetting("Settings:StravaAccessToken", stravaAccessToken);
                    SettingsHelpers.AddOrUpdateAppSetting("Settings:StravaRefreshToken", stravaRefreshToken);
                    SettingsHelpers.AddOrUpdateAppSetting("Settings:StravaTokenExpire", stravaTokenExpire);
                    _settings.StravaAccessToken = stravaAccessToken;
                    _settings.StravaRefreshToken = stravaAccessToken;
                    _settings.StravaTokenExpire = stravaTokenExpire;

                    Console.WriteLine($"Strava access token = {stravaAccessToken}");
                    return stravaAccessToken;
                }
                Console.WriteLine(stravaResponse.Content.ReadAsStringAsync().Result);
                return string.Empty;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return string.Empty;
            }
        }




        private string AuthrizeToStrava()
        {
            using HttpListener httpListener = new();
            string redirectPath = $"/Temporary_Listen_Addresses/{Guid.NewGuid()}/";
            httpListener.Prefixes.Add("http://+:80" + redirectPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.strava.com/oauth/authorize?" +
                $"client_id={_clientId}&" +
                $"redirect_uri=http://localhost//{redirectPath}&" +
                "response_type=code&" +
                "scope=activity:read_all,activity:write,profile:write",
                UseShellExecute = true
            });

            httpListener.Start();
            Console.WriteLine("Waiting for Strava authentication...");

            HttpListenerContext context = httpListener.GetContext();
            string stravaCode = context.Request.QueryString["code"] ?? throw new("! Strava 'code' missing in the http redirect query.");

            HttpListenerResponse response = context.Response;
            byte[] buffer = Encoding.UTF8.GetBytes("<html><body><h1>Authorization successful!</h1></body></html>");
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            Console.WriteLine($"Strava code = {stravaCode}");
            httpListener.Stop();

            SettingsHelpers.AddOrUpdateAppSetting("Settings:StravaCode", stravaCode);

            return stravaCode;
        }


        private async Task<string> RefreshAccessToken(string refresh_token)
        {

            try
            {
                using HttpClient stravaHttpClient = new()
                {
                    BaseAddress = new Uri("https://www.strava.com")
                };

                //Example cURL Request

                //curl -X POST https://www.strava.com/api/v3/oauth/token \
                //  -d client_id = ReplaceWithClientID \
                //  -d client_secret = ReplaceWithClientSecret \
                //  -d grant_type = refresh_token \
                //  -d refresh_token = ReplaceWithRefreshToken


                string authUrl = "/oauth/token?" +
                  $"client_id={_clientId}&" +
                  $"client_secret={_clientSecret}&" +
                  "grant_type=refresh_token&" +
                  $"refresh_token={refresh_token}"
                  ;
                HttpResponseMessage stravaResponse = await stravaHttpClient.PostAsync(authUrl, null);
                dynamic stravaAccessResponse = (await stravaResponse.Content.ReadAsStringAsync()).JsonDeserialize();
                string stravaAccessToken = stravaAccessResponse.access_token;
                string stravaRefreshToken = stravaAccessResponse.refresh_token;
                string stravaTokenExpire = stravaAccessResponse.expires_at;

                SettingsHelpers.AddOrUpdateAppSetting("Settings:StravaAccessToken", stravaAccessToken);
                SettingsHelpers.AddOrUpdateAppSetting("Settings:StravaRefreshToken", stravaAccessToken);
                SettingsHelpers.AddOrUpdateAppSetting("Settings:StravaTokenExpire", stravaTokenExpire);

                Console.WriteLine($"Strava access token = {stravaAccessToken}");
                return stravaAccessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return string.Empty;
            }
        }


        void checkStravaApiLimits(HttpResponseMessage stravaResponse)
        {
            if (stravaResponse.Headers.TryGetValues("X-Ratelimit-Usage", out var headersUsage) &&
                 stravaResponse.Headers.TryGetValues("X-Ratelimit-Limit", out var headersLimit))
            {
                stravaApiUsages = headersUsage.First().Split(',');
                stravaApiLimits = headersLimit.First().Split(',');

                if (int.Parse(stravaApiUsages[0]) >= int.Parse(stravaApiLimits[0]))
                    throw new($"! 15-minute Strava API limit {stravaApiLimits[0]} has been exhausted.");
                if (int.Parse(stravaApiUsages[1]) >= int.Parse(stravaApiLimits[1]))
                    throw new($"! Daily Strava API limit {stravaApiLimits[1]} has been exhausted.");
            }
        }
    }
}
