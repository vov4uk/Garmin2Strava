using ConsoleTables;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StravaActivity = Strava.Activities.Activity;


internal static class Program
{
    [STAThread]
    static async Task Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;
        var services = new ServiceCollection();
        ConfigureServices(services);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetService<Settings>();

        TimeSpan maxGarminStravaTimeDifference = new(0, 5, 0);

        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        var garmin = serviceProvider.GetService<IGarminClient>();
        await garmin.AuthorizeAsync();
        var garminActivities = await garmin.GetActivitiesListAsync().ConfigureAwait(false);

        var table = new ConsoleTable("Activity Type", "Start Time", "Activity Name", "Status");
        foreach (var activity in garminActivities)
        {
            var downloaded = await garmin.DownloadActivityAsync(activity.ActivityId, settings.Garmin.GarminActivitiesPath);
            var status = downloaded ? "Downloaded" : "Skipped";
            table.AddRow(activity.ActivityType.TypeKey, activity.StartTimeLocal, activity.ActivityName, status);
        }

        table.Write(Format.Minimal);

        var stravaClient = serviceProvider.GetService<IStravaClient>();
        var stravaActivities = await stravaClient.GetActivitiesListAsync().ConfigureAwait(false);

        Log.Information("\r\nCompare Garmin 2 Strava activities\r\n");

        var resultsTable = new ConsoleTable("Activity Type", "Start Time", "Activity Name", "Activity ID", "Strava ID", "Status");

        foreach (GarminActivity garminActivity in garminActivities)
        {
            var foundGarminInStrava =
              from StravaActivity stravaActivity
              in stravaActivities
              where (garminActivity.StartTimeGmt.ToLocalTime() - stravaActivity.DateTimeStart.ToLocalTime()).Duration() < maxGarminStravaTimeDifference ||
                    (garminActivity.StartTimeLocal.ToLocalTime() - stravaActivity.DateTimeStartLocal.ToLocalTime()).Duration() < maxGarminStravaTimeDifference
              select stravaActivity;

            if (foundGarminInStrava.Count() != 1)
            {
                var stravaActivityId = await stravaClient.UploadActivityAsync(Path.Combine(settings.Garmin.GarminActivitiesPath, $"{garminActivity.ActivityId}_ACTIVITY.fit"));
                await stravaClient.UpdateActivityAsync(stravaActivityId, garminActivity.ActivityName, garminActivity.Description);
                resultsTable.AddRow(garminActivity.ActivityType.TypeKey, garminActivity.StartTimeLocal, garminActivity.ActivityName, garminActivity.ActivityId, stravaActivityId, "Uploaded");
            }
            else
            {
                var stravaActivity = foundGarminInStrava.First();
                string status = "Found";
                if (settings.UpdateName && garminActivity.ActivityName != stravaActivity.Name)
                {
                    await stravaClient.UpdateActivityAsync(stravaActivity.Id, garminActivity.ActivityName, garminActivity.Description);
                    status = "Updated";
                }
                resultsTable.AddRow(garminActivity.ActivityType.TypeKey, garminActivity.StartTimeLocal, garminActivity.ActivityName, garminActivity.ActivityId, stravaActivity.Id, status);
            }
        }


        resultsTable.Write(Format.Minimal);


        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\r\nDone for today!");
        Console.ResetColor();

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