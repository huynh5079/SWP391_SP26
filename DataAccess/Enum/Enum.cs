using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Enum
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum UploadContext
    {
        Avatar,
        Certificate,        // Chứng chỉ gia sư
        IdentityDocument,   // CCCD/CMND
        Material,           // Tài liệu học tập
        LessonVideo,        // Video bài học
        Chat                // File/hình ảnh trong chat
    }

    public enum ApprovalActionEnum
    {
        Approve,       // Đồng ý
        Reject,        // Từ chối
        RequestChange  // Yêu cầu chỉnh sửa lại
    }

    public enum ProposalStatusEnum
    {
        Pending,    // Đang chờ duyệt
        Approved,   // Đã duyệt (Cho phép chi)
        Rejected    // Từ chối
    }

    public enum ScanTypeEnum
    {
        CheckIn,
        CheckOut
    }

    public enum EventTypeEnum
    {
        Workshop,
        Seminar,      // Hội thảo
        Competition,  // Cuộc thi
        Training,     // Đào tạo
        Social        // Hoạt động xã hội/ngoại khóa
    }

    public enum EventStatusEnum
    {
        Draft,      // Nháp (Chưa gửi duyệt)
        Pending,    // Chờ duyệt (Đã gửi lên cấp trên)
        Approved,   // Đã duyệt (Sắp diễn ra)
        Rejected,   // Bị từ chối
        Happening,  // Đang diễn ra
        Completed,  // Đã kết thúc thành công
        Cancelled   // Đã bị hủy
    }

    public enum ExpenseStatusEnum
    {
        Pending,    // Mới nộp, chờ kiểm tra
        Accepted,   // Hóa đơn hợp lệ, chấp nhận thanh toán
        Rejected    // Hóa đơn không hợp lệ (mờ, sai số tiền...)
    }

    public enum SemesterStatusEnum
    {
        Upcoming, // Sắp diễn ra
        Active,   // Đang diễn ra (Học kỳ hiện tại)
        Finished  // Đã kết thúc
    }

    public enum TeamRoleEnum
    {
        Leader, // Trưởng nhóm
        Member  // Thành viên
    }

    public enum TicketStatusEnum
    {
        Registered, // Đã đăng ký thành công
        CheckedIn,  // Đã điểm danh (tham gia sự kiện)
        Cancelled   // Đã hủy vé
    }

    public enum UserStatusEnum
    {
        Active,     // Đang hoạt động
        Inactive,   // Tạm khóa
        Banned,      // Bị cấm vĩnh viễn
        Pending     // Chờ xác nhận (Email/Admin)
    }
}
