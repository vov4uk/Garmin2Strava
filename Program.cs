using Garmin.Connect.Models;
using Garmin2StravaFinalSync.Garmin;
using Garmin2StravaFinalSync.Strava;
using Garmin2StravaFinalSync.Strava.Abstract;
using Garmin2StravaFinalSync.Strava.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StravaActivity = Strava.Activities.Activity;


internal static class Program
{
    [STAThread]
    static async Task Main()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetService<Settings>();

        TimeSpan maxGarminStravaTimeDifference = new(0, 5, 0);

        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        DateTime to = DateTime.Today;
        DateTime from = new DateTime(Math.Max(settings.MinActivityDate.Ticks, to.AddDays(settings.PeriodDays * -1).Ticks));

        Log.Information($"Time interval = {from:yyyy-MM-dd} - {to:yyyy-MM-dd}");

        var garmin = serviceProvider.GetService<IGarminClient>();
        await garmin.AuthorizeAsync();
        var garminActivities = await garmin.GetActivitiesListAsync(from, to).ConfigureAwait(false);

        foreach (var activity in garminActivities)
        {
            await garmin.DownloadActivityAsync(activity.ActivityId, settings.Garmin.GarminActivitiesPath);
        }

        var stravaClient = serviceProvider.GetService<IStravaClient>();
        var stravaActivities = await stravaClient.GetActivitiesListAsync(from, to).ConfigureAwait(false);

        foreach (GarminActivity garminActivity in garminActivities)
        {
            Log.Information(
              $"\t{garminActivity.ActivityType.TypeKey}\t" +
              $"{garminActivity.StartTimeLocal}\t" +
              $"{garminActivity.ActivityName}");

            var foundGarminInStrava =
              from StravaActivity stravaActivity
              in stravaActivities
              where (garminActivity.StartTimeGmt.ToLocalTime() - stravaActivity.DateTimeStart.ToLocalTime()).Duration() < maxGarminStravaTimeDifference ||
                    (garminActivity.StartTimeLocal.ToLocalTime() - stravaActivity.DateTimeStartLocal.ToLocalTime()).Duration() < maxGarminStravaTimeDifference
              select stravaActivity;

            if (foundGarminInStrava.Count() != 1)
            {
                Log.Information($"\t! Garmin activity {garminActivity.ActivityId} not found in Strava!");
                var stravaActivityId = await stravaClient.UploadActivityAsync(Path.Combine(settings.Garmin.GarminActivitiesPath, $"{garminActivity.ActivityId}_ACTIVITY.fit"));

                await stravaClient.UpdateActivityAsync(stravaActivityId, garminActivity.ActivityName, garminActivity.Description);
            }
            else
            {
                var stravaActivity = foundGarminInStrava.First();
                Log.Information($"\t! Garmin activity {garminActivity.ActivityId} found in Strava with id {stravaActivity.Id}!");
                if (settings.UpdateName && garminActivity.ActivityName != stravaActivity.Name)
                {
                    Log.Information($"\t! Strava activity {stravaActivity.Name} -> {garminActivity.ActivityName}!");
                    await stravaClient.UpdateActivityAsync(stravaActivity.Id, garminActivity.ActivityName, garminActivity.Description);
                }
            }
        }
    }

    public static void ConfigureServices(ServiceCollection services)
    {
        string logName = Path.Combine(Directory.GetCurrentDirectory(), "strava_uploader.log");
        string configName = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(logName)
            .WriteTo.Console()
            .CreateLogger();

        Settings settings = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build()
            .GetRequiredSection("Settings")
            .Get<Settings>();


        services.AddSingleton<Settings>(_ => settings);
        services.AddSingleton<StravaConfig>(_ => settings.Strava);
        services.AddSingleton<GarminConfig>(_ => settings.Garmin);

        services.AddScoped<IAuthListener, AuthListener>();
        services.AddScoped<IKeyRepository, KeyRepository>();
        services.AddScoped<IStravaClient, StravaClient>();
        services.AddScoped<IGarminClient, GarminClient>();

        services.AddHttpClient();
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
    }
}