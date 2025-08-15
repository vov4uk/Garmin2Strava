using System;
using System.Collections.Generic;
using System.Dynamic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

internal static class Extensions
{
    public static double DateTimeToUnixTimestamp(this DateTime dateTime) => (TimeZoneInfo.ConvertTimeToUtc(dateTime) - DateTime.UnixEpoch).TotalSeconds;

    public static dynamic JsonDeserialize(this string json) => JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());

    public static List<ExpandoObject> JsonDeserializeList(this string json) => JsonConvert.DeserializeObject<List<ExpandoObject>>(json, new ExpandoObjectConverter());
}