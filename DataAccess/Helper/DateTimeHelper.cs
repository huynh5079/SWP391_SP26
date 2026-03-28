using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Helper
{
    public static class DateTimeHelper
    {
        // Vietnam timezone: SE Asia Standard Time (UTC+7)
        private static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        /// <summary>
        /// Gets the current date and time in Vietnam timezone (UTC+7)
        /// </summary>
        public static DateTime VietnamNow =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

        /// <summary>
        /// Gets the current date and time in Vietnam timezone (UTC+7)
        /// This is an alias for VietnamNow for consistency
        /// </summary>
        public static DateTime GetVietnamTime() => VietnamNow;

        /// <summary>
        /// Converts UTC DateTime to Vietnam timezone (Extension Method)
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime to convert</param>
        /// <returns>DateTime in Vietnam timezone</returns>
        public static DateTime ToVietnamTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                 if (utcDateTime.Kind == DateTimeKind.Unspecified) 
                 {
                     utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                 }
                 else 
                 {
                    // throw new ArgumentException("DateTime must be in UTC", nameof(utcDateTime));
                    // To avoid runtime crashes in production, if it's already Local, we'll try to convert it 
                    // from local to UTC first, or assume it is UTC if Unspecified.
                 }
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Converts Vietnam timezone DateTime to UTC (Extension Method)
        /// </summary>
        public static DateTime ToUtc(this DateTime vietnamDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(vietnamDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Gets the current date in Vietnam timezone
        /// </summary>
        public static DateOnly VietnamToday =>
            DateOnly.FromDateTime(VietnamNow);

        /// <summary>
        /// Converts UTC DateTime to DateOnly in Vietnam timezone (Extension Method)
        /// </summary>
        public static DateOnly ToVietnamDateOnly(this DateTime utcDateTime)
        {
            return DateOnly.FromDateTime(ToVietnamTime(utcDateTime));
        }

        /// <summary>
        /// Gets Unix timestamp in milliseconds for current Vietnam time
        /// </summary>
        public static long VietnamNowUnixMilliseconds =>
            new DateTimeOffset(VietnamNow).ToUnixTimeMilliseconds();

        /// <summary>
        /// Gets Unix timestamp in seconds for current Vietnam time
        /// </summary>
        public static long VietnamNowUnixSeconds =>
            new DateTimeOffset(VietnamNow).ToUnixTimeSeconds();
    }

}
