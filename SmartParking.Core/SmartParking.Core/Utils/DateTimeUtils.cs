using System;

namespace SmartParking.Core.Utils
{
    public static class DateTimeUtils
    {
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

        /// <summary>
        /// Converts UTC DateTime to Vietnam time (GMT+7)
        /// </summary>
        public static DateTime ToVietnamTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                // If the datetime is not explicitly UTC, assume it is and convert
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Converts local Vietnam time to UTC
        /// </summary>
        public static DateTime ToUtcFromVietnam(this DateTime vietnamDateTime)
        {
            if (vietnamDateTime.Kind != DateTimeKind.Unspecified)
            {
                // If the datetime has a kind specified, unspecify it first
                vietnamDateTime = DateTime.SpecifyKind(vietnamDateTime, DateTimeKind.Unspecified);
            }
            
            return TimeZoneInfo.ConvertTimeToUtc(vietnamDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Gets the current time in Vietnam timezone
        /// </summary>
        public static DateTime GetVietnamNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        /// <summary>
        /// Formats a DateTime for display in Vietnam format
        /// </summary>
        public static string FormatVietnamDateTime(this DateTime dateTime)
        {
            var vietnamTime = dateTime.Kind == DateTimeKind.Utc 
                ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, VietnamTimeZone) 
                : dateTime;
                
            return vietnamTime.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }
}
