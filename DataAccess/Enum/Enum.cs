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
		Chat,                // File/hình ảnh trong chat
		EventThumbnail,
		EventDocument
	}

	public enum ApprovalActionEnum
	{
		Approve,       // Đồng ý
		Reject,        // Từ chối
		RequestChange,  // Yêu cầu chỉnh sửa lại
		NYA	//Not yet approved(for Organizer)
	}

	public enum ChatSessionStatus
	{
		Active,    // Đang hoạt động
		Archived   // Đã lưu trữ
	}

	public enum ChatMessageStatus
	{
		Streaming, // Đang truyền tải
		Final,     // Hoàn tất
		Error      // Lỗi
	}

	public enum ProposalStatusEnum
	{
        Draft,    // Organizer đang soạn thảo
        Pending,    // Đang chờ duyệt
		Approved,   // Đã duyệt (Cho phép chi)
		Rejected,    // Từ chối
		
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
		Social,        // Hoạt động xã hội/ngoại khóa
		Bootcamp       // Trại huấn luyện
	}

	public enum EventStatusEnum
	{
		Draft,      // Nháp (Chưa gửi duyệt)
		Pending,    // Chờ duyệt (Đã gửi lên cấp trên)
		Approved,   // Đã duyệt (Sắp diễn ra)
		Rejected,   // Bị từ chối
		Published,  // Đã công khai (Đã duyệt và có thể hiển thị cho người dùng)
		Upcoming,   // Sắp diễn ra
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
		Finished, // Đã kết thúc
	
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
		Cancelled,   // Đã hủy vé
		Used        // Đã sử dụng (sau khi checkout)
			}

	public enum UserStatusEnum
	{
		Active,     // Đang hoạt động
		Inactive,   // Tạm khóa
		Banned,      // Bị cấm vĩnh viễn
		Pending     // Chờ xác nhận (Email/Admin)
	}

	public enum SystemLogStatusEnum
	{
		Success = 200,
		BadRequest = 400,
		Unauthorized = 401,
		Forbidden = 403,
		NotFound = 404,
		ServerError = 500
	}

	public enum LocationStatusEnum
	{
		Available,    // phòng trống
		Maintenance,  // đang bảo trì
		Occupied,     // đang có người dùng
		Closed        // phòng bị đóng / không cho sử dụng
	}

	public enum LocationTypeEnum
	{
		Room,
		Hall,
		Lab,
		Auditorium,
		Outdoor,
		Online
	}
	//hình thức tổ chức
	public enum EventModeEnum
	{
		Offline,
		Online,
		Hybrid
	}public enum EventWaitlistStatusEnum
	{
		Waiting, //đang chờ

		Offered, //được mời đăng ký vì có slot trống

		Accepted, //đã nhận slot

		Expired, //quá hạn không phản hồi

		Cancelled, //người dùng tự rời
	}
	
	public enum EventStatusAvailableEnum { 
		Available,
		NA //not available
	}
	public enum QuestionSetEnum
	{
		Available,
		NA //not available
	}
	public enum QuizTypeEnum
	{
	    Practice,
		Exam,
		Survey,
		LiveQuiz
	}
	public enum QuizStatusEnum
	{
		Draft, //nháp
		Published,//đang làm bài
		Closed//đã xong
	}
	public enum QuizSetVisibilityEnum
	{
		Private,
		Public
	}
	public enum QuizTimeTypeEvent { 
		AfterEvent,
		InEvent,
		BeforeEvent
	}
	public enum QuestionTypeOptionEnum
	{
		SingleChoice,
		MultipleChoice,
		TrueFalse,
		ShortAnswer
	}

	public enum QuestionDifficultyEnum
	{
		Easy,
		Medium,
		Hard
	}

	public enum StudentQuizScoreStatusEnum
	{
		NotStarted,
		InProgress,
		Submitted,
		Graded
	}
	public enum QuizBankSourceTypeEnum
	{
		Organizer,
		Community
	}

	public enum FeedbackStatusEnum
	{
	    NA, 
		BeforeEvent,
		DuringEvent,   
		AfterEvent
	}
	public enum FeedBackRatingsEnum
	{
		OneStar = 1,
		TwoStars = 2,
		ThreeStar = 3,
		FourStar = 4,
		FiveStar = 5,
	}
	public enum AppriciateEventEnum
	{
		Positive,
		Neutral,
		Negative
	}

}
