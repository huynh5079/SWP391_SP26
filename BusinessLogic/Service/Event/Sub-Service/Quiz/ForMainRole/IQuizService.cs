using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuizScores;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.QuizActions;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UpdateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UploadQuizFile;

namespace BusinessLogic.Service.Event.Sub_Service.Quiz
{
    public interface IQuizService
    {
        /// <summary>
        /// Tạo mới quiz cho một event (có thể từ question bank hoặc tạo trống ban đầu).
        /// </summary>
        Task<CreateQuizSetResponseDto> CreateQuizSetAsync(CreateQuizSetRequestDto request);

        /// <summary>
        /// Lấy danh sách question bank mà organizer có thể sử dụng.
        /// </summary>
        Task<GetAvailableQuizBanksResponseDto> GetAvailableQuizBanksAsync(GetAvailableQuizBanksRequestDto request);

        /// <summary>
        /// Thêm một câu hỏi mới vào quiz.
        /// </summary>
        Task<AddQuizQuestionResponseDto> AddQuizQuestionAsync(AddQuizQuestionRequestDto request);

        /// <summary>
        /// Lấy thông tin chi tiết quiz kèm danh sách câu hỏi.
        /// </summary>
        Task<GetQuizDetailResponseDto?> GetQuizDetailAsync(GetQuizDetailRequestDto request);

        /// <summary>
        /// Lấy danh sách quiz của organizer theo bộ lọc.
        /// </summary>
        Task<GetOrganizerQuizzesResponseDto> GetOrganizerQuizzesAsync(GetOrganizerQuizzesRequestDto request);

        /// <summary>
        /// Lấy điểm của tất cả học sinh đã làm một quiz.
        /// </summary>
        Task<GetQuizScoresResponseDto> GetQuizScoresAsync(GetQuizScoresRequestDto request);

        /// <summary>
        /// Lấy điểm quiz của một học sinh cụ thể.
        /// </summary>
        Task<GetStudentQuizScoreResponseDto?> GetStudentQuizScoreAsync(GetStudentQuizScoreRequestDto request);

        /// <summary>
        /// Xem trước nội dung quiz trước khi publish.
        /// </summary>
        Task<PreviewQuizResponseDto> PreviewQuizAsync(PreviewQuizRequestDto request);

        /// <summary>
        /// Publish quiz để học sinh có thể bắt đầu làm bài.
        /// </summary>
        Task<PublishQuizResponseDto> PublishQuizAsync(PublishQuizRequestDto request);

        /// <summary>
        /// Cập nhật trạng thái chia sẻ (public/private) của quiz set.
        /// </summary>
        Task<PublishQuizSetResponseDto> PublishQuizSetAsync(PublishQuizSetRequestDto request);

        /// <summary>
        /// Đóng quiz (ngừng cho phép làm bài).
        /// </summary>
        Task<CloseQuizResponseDto> CloseQuizAsync(CloseQuizRequestDto request);

        /// <summary>
        /// Xóa quiz nếu thỏa điều kiện nghiệp vụ.
        /// </summary>
        Task<DeleteQuizResponseDto> DeleteQuizAsync(DeleteQuizRequestDto request);

        /// <summary>
        /// Cập nhật nội dung một câu hỏi trong quiz.
        /// </summary>
        Task<UpdateQuizQuestionResponseDto> UpdateQuizQuestionAsync(UpdateQuizQuestionRequestDto request);

        /// <summary>
        /// Xóa một câu hỏi khỏi quiz.
        /// </summary>
        Task<DeleteQuizQuestionResponseDto> DeleteQuizQuestionAsync(DeleteQuizQuestionRequestDto request);

        /// <summary>
        /// Cập nhật thông tin chung của quiz set (title, type, topic, ...).
        /// </summary>
        Task<UpdateQuizSetResponseDto> UpdateQuizSetAsync(UpdateQuizSetRequestDto request);

        /// <summary>
        /// Upload file quiz và parse tự động thành danh sách câu hỏi.
        /// </summary>
        Task<UploadQuizFileResponseDto> UploadQuizFileAsync(UploadQuizFileRequestDto request);
    }
}
