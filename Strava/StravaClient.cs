using Garmin.Connect.Models;
using Garmin2StravaFinalSync.Strava.Abstract;
using Garmin2StravaFinalSync.Strava.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using StravaActivity = Strava.Activities.Activity;

namespace Garmin2StravaFinalSync.Strava
{
    public class StravaClient : IStravaClient
    {
        private const string authUrl = "https://www.strava.com/oauth/token";
        private const int totalAttempts = 3;
        private const string uploadUrl = "https://www.strava.com/api/v3/uploads";
        private readonly IAuthListener _authListener;
        private readonly IKeyRepository _authRepository;
        private readonly StravaConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StravaClient> _logger;
        private string[] stravaApiUsages = null, stravaApiLimits = null;

        public StravaClient(StravaConfig options, IKeyRepository repository, IHttpClientFactory httpClientFactory, IAuthListener authListener, ILogger<StravaClient> logger)
        {
            _config = options;
            _authRepository = repository;
            _httpClientFactory = httpClientFactory;
            _authListener = authListener;
            _logger = logger;
        }

        public async Task<List<StravaActivity>> GetActivitiesListAsync(DateTime from, DateTime to)
        {
            AuthTokenResponse auth = await GetAuthToken();
            return await GetActivitiesListAsync(auth.AccessToken, from, to);
        }

        public async Task<long> UploadActivityAsync(string path)
        {
            AuthTokenResponse auth = await GetAuthToken();
            return await UploadActivityAsync(auth.AccessToken, path);
        }

        public async Task UpdateActivityAsync(long activityId, string name, string description)
        {
            AuthTokenResponse auth = await GetAuthToken();
            await UpdateActivityAsync(auth.AccessToken, activityId, name, description);
        }

        private async Task UpdateActivityAsync(string token, long activityId, string name, string description)
        {
            HttpResponseMessage stravaResponse = null;

            var stravaHttpClient = _httpClientFactory.CreateClient();
            stravaHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (name != "" || description != "")
            {

                string url = $"https://www.strava.com/api/v3/activities/{activityId}?";

                if (!string.IsNullOrEmpty(name))
                {
                    url += $"&name={HttpUtility.UrlEncode(name)}";
                }
                if (!string.IsNullOrEmpty(description))
                {
                    url += $"&description={HttpUtility.UrlEncode(description)}";
                }

                stravaResponse = await stravaHttpClient.PutAsync(url, null);

                if (stravaResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("\tStrava activity updated OK");
                }
                else
                {
                    _logger.LogWarning($"\t! Error updating Strava activity {stravaResponse.StatusCode}!");
                }

                checkStravaApiLimits(stravaResponse);
            }
        }

        private async Task<AuthTokenResponse> GetAuthToken()
        {
            AuthTokenResponse auth;

            if (_authRepository.Exists())
            {
                _logger.LogInformation("Reading auth from file");
                auth = await _authRepository.GetAsync();
            }
            else
            {
                auth = await GetAuthTokenAsync();
                _logger.LogInformation("Saving auth to file");
                await _authRepository.SetAsync(auth);
            }

            if (auth.IsExpired)
            {
                _logger.LogInformation("Auth token has expired, getting a new one");
                auth = await GetRefreshTokenAsync(auth);
                await _authRepository.SetAsync(auth);
            }

            return auth;
        }

        private void checkStravaApiLimits(HttpResponseMessage stravaResponse)
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

        private async Task<string> GetAuthCodeAsync()
        {
            string url = $"https://www.strava.com/oauth/authorize?client_id=" +
                $"{_config.ClientId}" +
                $"&redirect_uri={_config.CallBackUrl}" +
                $"&scope={HttpUtility.UrlEncode(string.Join(',', _config.Scope))}" +
                $"&response_type={_config.ResponseType}";

            _logger.LogInformation("Opening browser for user confirmation");

            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url
            });

            return await _authListener.GetAuthCodeAsync();
        }

        private async Task<AuthTokenResponse> GetAuthTokenAsync()
        {
            string code = await GetAuthCodeAsync();
            var body = new AuthTokenRequest(_config, code);
            return await MakeTokenRequestAsync(body);
        }

        private async Task<AuthTokenResponse> GetRefreshTokenAsync(AuthTokenResponse auth)
        {
            var body = new AuthTokenRefreshRequest(_config, auth.RefreshToken);
            return await MakeTokenRequestAsync(body);
        }

        private async Task<AuthTokenResponse> MakeTokenRequestAsync(object body)
        {
            var request = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.PostAsync(authUrl, request);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                AuthTokenResponse response = await System.Text.Json.JsonSerializer.DeserializeAsync<AuthTokenResponse>(contentStream) ?? throw new("AuthTokenResponse not valid");
                return response;
            }
            else
            {
                throw new($"GetAuthToken returned with non sucess status code: {httpResponseMessage.StatusCode}");
            }
        }

        private async Task<long> UploadActivityAsync(string token, string fileName)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var stream = new FileStream(fileName, FileMode.Open);
            var form = new MultipartFormDataContent
            {
                { new StringContent("fit"), "data_type" },
                { new StreamContent(stream), "file", Path.GetFileName(fileName) }
            };

            var httpResponseMessage = await httpClient.PostAsync(uploadUrl, form);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                UploadResponse response = await System.Text.Json.JsonSerializer.DeserializeAsync<UploadResponse>(contentStream) ?? throw new("Upload response not valid");
                _logger.LogInformation("{fileName} has been uploaded with status: {Status}", fileName, response.Status);

                if (string.IsNullOrEmpty(response.Error))
                {
                    long activityId = await WaitForUploadAsync(response.Id, token);
                    _logger.LogInformation("{fileName} uploaded with activity id {activityId}", fileName, activityId);
                    stream.Close();
                    return activityId;
                }
                else
                {
                    throw new($"Upload failed with error {response.Error}");
                }
            }
            else
            {
                throw new($"Upload activity failed with status code: {httpResponseMessage.StatusCode}");
            }
        }

        private async Task<long> WaitForUploadAsync(long id, string token)
        {
            int attempts = 1;
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            while (attempts <= totalAttempts)
            {
                var httpResponseMessage = await httpClient.GetAsync($"{uploadUrl}/{id}");

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                    UploadResponse response = await System.Text.Json.JsonSerializer.DeserializeAsync<UploadResponse>(contentStream) ?? throw new("Status check response not valid");

                    if (response.Status == "Your activity is ready.")
                    {
                        return response.ActivityId ?? 0;
                    }
                    else if (!string.IsNullOrEmpty(response.Error))
                    {
                        throw new(response.Error);
                    }
                    else
                    {
                        attempts++;
                    }
                }
                else
                {
                    throw new($"Checking upload status failed with status: {httpResponseMessage.StatusCode}");
                }

                // Wait for 5 seconds and try again
                await Task.Delay(5000);
            }

            throw new($"Checking upload failed with max attempts ({totalAttempts}) reached");
        }

        private async Task<List<StravaActivity>> GetActivitiesListAsync(string token, DateTime from, DateTime to)
        {
            HttpResponseMessage stravaResponse = null;

            var stravaHttpClient = _httpClientFactory.CreateClient();
            stravaHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("Reading Strava activities, please wait...");
            List<StravaActivity> stravaActivities = new();
            for (int stravaActivitiesPage = 1; ; stravaActivitiesPage++)
            {
                string getActivitiesUrl = "https://www.strava.com/api/v3/athlete/activities?" +
                  $"before={to.AddDays(1).AddSeconds(-1).DateTimeToUnixTimestamp()}&" +
                  $"after={from.DateTimeToUnixTimestamp()}&" +
                  $"page={stravaActivitiesPage}&" +
                  "per_page=200&";

                stravaResponse = await stravaHttpClient.GetAsync(getActivitiesUrl);
                if (stravaResponse.StatusCode != HttpStatusCode.OK)
                    throw new($"! Error {stravaResponse.StatusCode} when reading Strava activities.");

                checkStravaApiLimits(stravaResponse);

                var activitiesResponse = await stravaResponse.Content.ReadAsStringAsync();

                List<StravaActivity> newActivities = JsonConvert.DeserializeObject<List<StravaActivity>>(activitiesResponse);
                if (!newActivities.Any())
                    break;

                foreach (StravaActivity stravaActivity in newActivities)
                {
                    _logger.LogInformation($"\t{stravaActivity.Type}\t{stravaActivity.StartDateLocal}\t" + $"{stravaActivity.Name}");
                }

                stravaActivities.AddRange(newActivities);
            }

            if (stravaActivities.Count == 0)
            {
                _logger.LogInformation($"No Strava activities");
            }
            return stravaActivities;
        }
    }
}
