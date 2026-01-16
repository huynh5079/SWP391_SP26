//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Claims;
//using System.Text;
//using System.Threading.Tasks;

//namespace BusinessLogic.Helper
//{
//    public static class ClaimsPrincipalExtensions
//    {
//        public static string? GetUserId(this ClaimsPrincipal user)
//        {
//            if (user == null) return null;

//            // thứ tự ưu tiên: uid -> sub -> NameIdentifier -> id
//            return user.FindFirstValue("uid")
//                ?? user.FindFirstValue("sub")
//                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
//                ?? user.FindFirstValue("id");
//        }

//        public static string RequireUserId(this ClaimsPrincipal user)
//        {
//            var uid = user.GetUserId();
//            if (string.IsNullOrWhiteSpace(uid))
//                throw new UnauthorizedAccessException("Thiếu thông tin người dùng (uid).");
//            return uid;
//        }
//    }

//}
