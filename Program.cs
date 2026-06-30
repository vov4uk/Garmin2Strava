using ConsoleTables;
using Garmin.Connect.Models;
using Garmin2StravaFinalSync;
using Garmin2StravaFinalSync.Garmin;
using Garmin2StravaFinalSync.Strava;
using Garmin2StravaFinalSync.Strava.Abstract;
using Garmin2StravaFinalSync.Strava.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StravaActivity = Strava.Activities.Activity;


internal static class Program
{
    static async Task Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;
        var services = new ServiceCollection();
        ConfigureServices(services);

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var buildDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local)
           .AddDays(version.Build).AddSeconds(version.Revision * 2);
        Log.Information("Garmin2Strava {buildDate}", "v." + buildDate.Date.ToString("yyyy.MM.dd"));

        try
        {
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var settings = serviceProvider.GetRequiredService<Settings>();

            TimeSpan maxGarminStravaTimeDifference = new(0, 5, 0);

            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            string stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "last_activity.json");
            var state = LoadState(stateFilePath);
            DateTime? lastActivityDate = state?.GetDate();

            var garmin = serviceProvider.GetRequiredService<IGarminClient>();
            await garmin.AuthorizeAsync();

            List<GarminActivity> garminActivities;
            if (lastActivityDate.HasValue)
            {
                var startDate = lastActivityDate.Value.AddDays(-1);
                Log.Information("Loading Garmin activities from {StartDate} (last activity date - 1 day)", startDate.ToString("yyyy.MM.dd"));
                garminActivities = await garmin.GetActivitiesListAsync(startDate, DateTime.Now).ConfigureAwait(false);
            }
            else
            {
                garminActivities = await garmin.GetActivitiesListAsync().ConfigureAwait(false);
            }

            var table = new ConsoleTable("Activity Type", "Start Time", "Activity Name", "Status");
            foreach (var activity in garminActivities)
            {
                var downloaded = await garmin.DownloadActivityAsync(activity, settings.Garmin.GarminActivitiesPath);
                var status = downloaded ? "Downloaded" : "Skipped";
                table.AddRow(activity.ActivityType.TypeKey, activity.StartTimeLocal, activity.ActivityName, status);
            }

            table.Write(Format.Minimal);

            if (garminActivities.Count > 0)
            {
                var latestDate = garminActivities.Max(a => a.StartTimeLocal).Date;
                SaveState(stateFilePath, LastActivityState.FromDate(latestDate));
                Log.Information("Saved last activity date: {Date}", latestDate.ToString("yyyy.MM.dd"));
            }

            var stravaClient = serviceProvider.GetRequiredService<IStravaClient>();
            List<StravaActivity> stravaActivities;
            if (lastActivityDate.HasValue)
            {
                var startDate = lastActivityDate.Value.AddDays(-1);
                stravaActivities = await stravaClient.GetActivitiesListAsync(startDate).ConfigureAwait(false);
            }
            else
            {
                stravaActivities = await stravaClient.GetActivitiesListAsync().ConfigureAwait(false);
            }

            var stravaTable = new ConsoleTable("Activity Type", "Start Date", "Activity Name");
            foreach (var stravaActivity in stravaActivities)
            {
                stravaTable.AddRow(stravaActivity.Type, stravaActivity.StartDateLocal, stravaActivity.Name);
            }
            stravaTable.Write(Format.Minimal);

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
                    var stravaActivityId = await stravaClient.UploadActivityAsync(garminActivity.FullPath_Fit(settings.Garmin.GarminActivitiesPath));
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

        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unhandled exception occurred.");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Log.Information("\r\nDone for today!");
        Console.ResetColor();

        Console.ReadKey();
    }

    private static LastActivityState LoadState(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            return JsonConvert.DeserializeObject<LastActivityState>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    private static void SaveState(string path, LastActivityState state)
    {
        File.WriteAllText(path, JsonConvert.SerializeObject(state, Formatting.Indented));
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