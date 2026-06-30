using System;
using System.Globalization;

namespace Garmin2StravaFinalSync
{
    public class LastActivityState
    {
        private const string DateFormat = "yyyy.MM.dd";

        public string LastActivityDate { get; set; }

        public DateTime? GetDate()
        {
            if (string.IsNullOrEmpty(LastActivityDate))
                return null;
            if (DateTime.TryParseExact(LastActivityDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            return null;
        }

        public static LastActivityState FromDate(DateTime date) =>
            new() { LastActivityDate = date.ToString(DateFormat, CultureInfo.InvariantCulture) };
    }
}
