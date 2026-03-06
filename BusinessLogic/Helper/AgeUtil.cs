using BusinessLogic.Helper;
using DataAccess.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Helper
{
    public static class AgeUtil
    {
        private const int MinYear = 1950; // Có thể đưa vào cấu hình nếu muốn

        /// <summary>
        /// Validate ngày sinh cơ bản (DateOnly):
        /// - Không ở tương lai
        /// - Năm >= MinYear (mặc định 1900)
        /// </summary>
        public static void ValidateDob(DateOnly? dob)
        {
            if (!dob.HasValue) return;

            var date = dob.Value;
            var today = DateTimeHelper.VietnamToday;

            if (date > today)
                throw new ArgumentException("ngày sinh không được ở tương lai");

            if (date.Year < MinYear)
                throw new ArgumentException("ngày sinh không hợp lệ");
        }

        /// <summary>
        /// Tính tuổi (số nguyên) từ DateOnly tại thời điểm today (mặc định: UTC hôm nay).
        /// Trả về null nếu dob = null.
        /// </summary>
        public static int? CalculateAge(DateOnly? dob, DateOnly? today = null)
        {
            if (!dob.HasValue) return null;

            var birth = dob.Value;
            var now = today ?? DateTimeHelper.VietnamToday;

            var age = now.Year - birth.Year;
            // Nếu chưa đến sinh nhật năm nay thì trừ 1
            if (new DateOnly(now.Year, birth.Month, birth.Day) > now) age--;

            return age;
        }

        /// <summary>
        /// Kiểm tra dưới độ tuổi tối thiểu.
        /// </summary>
        public static bool IsMinor(DateOnly? dob, int minAge)
            => CalculateAge(dob) is int age && age < minAge;

        // ====== Backward-compatible overloads (DateTime?) ======

        /// <summary>
        /// Validate DOB cho DateTime? (chuyển nội bộ sang DateOnly).
        /// </summary>
        public static void ValidateDob(DateTime? dob)
            => ValidateDob(ToDateOnly(dob));

        /// <summary>
        /// Tính tuổi cho DateTime? (chuyển nội bộ sang DateOnly).
        /// </summary>
        public static int? CalculateAge(DateTime? dob, DateTime? today = null)
        {
            var birth = ToDateOnly(dob);
            var now = today.HasValue ? DateOnly.FromDateTime(today.Value) : (DateOnly?)null;
            return CalculateAge(birth, now);
        }

        /// <summary>
        /// Kiểm tra dưới độ tuổi tối thiểu từ DateTime? (chuyển nội bộ sang DateOnly).
        /// </summary>
        public static bool IsMinor(DateTime? dob, int minAge)
            => IsMinor(ToDateOnly(dob), minAge);

        // ====== Helpers chuyển đổi ======

        public static DateOnly? ToDateOnly(DateTime? dt)
            => dt.HasValue ? DateOnly.FromDateTime(dt.Value.Date) : (DateOnly?)null;

        public static DateTime? ToDateTime(DateOnly? d)
            => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;

        /// <summary>
        /// Parse chuỗi "dd-MM-yyyy" sang DateOnly.
        /// </summary>
        public static bool TryParseDob(string? value, out DateOnly dob)
            => DateOnly.TryParseExact(value ?? string.Empty, "dd-MM-yyyy", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out dob);
    }

}
