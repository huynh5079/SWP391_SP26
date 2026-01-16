//using DataAccess.Enum;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;

//namespace BusinessLogic.Helper
//{
//    public static class AvatarHelper
//    {
//        // DiceBear API v9.x với CDN Cloudflare - nhanh và ổn định
//        // Style: avataaars (cartoon Bitmoji-style)
//        private const string Base = "https://api.dicebear.com/9.x/avataaars/svg";

//        /// <summary>
//        /// Tạo avatar cho Student (random seed)
//        /// </summary>
//        public static string ForStudent()
//        {
//            var seed = Guid.NewGuid().ToString("N")[..8];
//            return $"{Base}?seed={seed}";
//        }

//        /// <summary>
//        /// Tạo avatar cho Parent (random seed)
//        /// </summary>
//        public static string ForParent()
//        {
//            var seed = Guid.NewGuid().ToString("N")[..8];
//            return $"{Base}?seed={seed}";
//        }

//        /// <summary>
//        /// Tạo avatar cho Tutor theo giới tính (dùng prefix seed để tạo style khác)
//        /// </summary>
//        public static string ForTutor(Gender? gender)
//        {
//            var seed = Guid.NewGuid().ToString("N")[..8];

//            // Dùng prefix seed để tạo ra style khác nhau cho male/female
//            return gender switch
//            {
//                Gender.Female => $"{Base}?seed=female_{seed}",
//                Gender.Male => $"{Base}?seed=male_{seed}",
//                _ => $"{Base}?seed={seed}"
//            };
//        }

//        /// <summary>
//        /// Tạo avatar với seed cố định (dùng userId để avatar consistent)
//        /// </summary>
//        public static string ForUser(string userId, Gender? gender = null)
//        {
//            return gender switch
//            {
//                Gender.Female => $"{Base}?seed=f_{userId}",
//                Gender.Male => $"{Base}?seed=m_{userId}",
//                _ => $"{Base}?seed={userId}"
//            };
//        }

//        /// <summary>
//        /// Validate avatar file (type and size)
//        /// </summary>
//        /// <param name="file">Avatar file to validate</param>
//        /// <exception cref="ArgumentException">Thrown when file is invalid</exception>
//        public static void ValidateAvatarFile(Microsoft.AspNetCore.Http.IFormFile file)
//        {
//            if (file == null || file.Length == 0)
//                throw new ArgumentException("File avatar không được để trống.");

//            // Check file size (20MB max)
//            const long maxSize = 20_000_000; // 20MB
//            if (file.Length > maxSize)
//                throw new ArgumentException($"File avatar quá lớn. Kích thước tối đa: {maxSize / 1_000_000}MB.");

//            // Check file type
//            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
//            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

//            if (!allowedExtensions.Contains(extension))
//                throw new ArgumentException($"Định dạng file không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}");

//            // Check MIME type
//            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
//            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
//                throw new ArgumentException("Loại file không hợp lệ. Chỉ chấp nhận file ảnh (JPG, PNG, WEBP).");
//        }
//    }

//}
