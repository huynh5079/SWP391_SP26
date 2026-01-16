using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Helper
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
        /// Converts UTC DateTime to Vietnam timezone
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime to convert</param>
        /// <returns>DateTime in Vietnam timezone</returns>
        public static DateTime ToVietnamTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be in UTC", nameof(utcDateTime));
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Converts Vietnam timezone DateTime to UTC
        /// </summary>
        /// <param name="vietnamDateTime">Vietnam DateTime to convert</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime ToUtc(DateTime vietnamDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(vietnamDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Gets the current date in Vietnam timezone
        /// </summary>
        public static DateOnly VietnamToday =>
            DateOnly.FromDateTime(VietnamNow);

        /// <summary>
        /// Converts UTC DateTime to DateOnly in Vietnam timezone
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime to convert</param>
        /// <returns>DateOnly in Vietnam timezone</returns>
        public static DateOnly ToVietnamDateOnly(DateTime utcDateTime)
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
