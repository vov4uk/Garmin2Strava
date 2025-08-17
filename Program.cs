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

        var garmin = serviceProvider.GetService<IGarminClient>();
        await garmin.AuthorizeAsync();
        var garminActivities = await garmin.GetActivitiesListAsync().ConfigureAwait(false);

        foreach (var activity in garminActivities)
        {
            await garmin.DownloadActivityAsync(activity.ActivityId, settings.Garmin.GarminActivitiesPath, $"{activity.ActivityType.TypeKey}\t{activity.StartTimeLocal}\t{activity.ActivityName}");
        }

        var stravaClient = serviceProvider.GetService<IStravaClient>();
        var stravaActivities = await stravaClient.GetActivitiesListAsync().ConfigureAwait(false);

        Log.Information("\r\nCompare Garmin 2 Strava activities\r\n");

        foreach (GarminActivity garminActivity in garminActivities)
        {
            string garminActivityTitle = $"{garminActivity.ActivityType.TypeKey}\t{garminActivity.StartTimeLocal}\t{garminActivity.ActivityName}\t";

            var foundGarminInStrava =
              from StravaActivity stravaActivity
              in stravaActivities
              where (garminActivity.StartTimeGmt.ToLocalTime() - stravaActivity.DateTimeStart.ToLocalTime()).Duration() < maxGarminStravaTimeDifference ||
                    (garminActivity.StartTimeLocal.ToLocalTime() - stravaActivity.DateTimeStartLocal.ToLocalTime()).Duration() < maxGarminStravaTimeDifference
              select stravaActivity;

            if (foundGarminInStrava.Count() != 1)
            {
                Log.Information($"{garminActivityTitle}not found in Strava!");
                var stravaActivityId = await stravaClient.UploadActivityAsync(Path.Combine(settings.Garmin.GarminActivitiesPath, $"{garminActivity.ActivityId}_ACTIVITY.fit"));

                await stravaClient.UpdateActivityAsync(stravaActivityId, garminActivity.ActivityName, garminActivity.Description);
            }
            else
            {
                var stravaActivity = foundGarminInStrava.First();
                Log.Information($"{garminActivityTitle}found in Strava with id {stravaActivity.Id}!");
                if (settings.UpdateName && garminActivity.ActivityName != stravaActivity.Name)
                {
                    Log.Information($"! Strava activity renamed {stravaActivity.Name} -> {garminActivity.ActivityName}!");
                    await stravaClient.UpdateActivityAsync(stravaActivity.Id, garminActivity.ActivityName, garminActivity.Description);
                }
            }
        }

        Log.Information("Done for today!");
        Console.ReadKey();
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