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
        Task<CreateQuizSetResponseDto> CreateQuizSetAsync(CreateQuizSetRequestDto request);
        Task<GetAvailableQuizBanksResponseDto> GetAvailableQuizBanksAsync(GetAvailableQuizBanksRequestDto request);
        Task<AddQuizQuestionResponseDto> AddQuizQuestionAsync(AddQuizQuestionRequestDto request);
        Task<GetQuizDetailResponseDto?> GetQuizDetailAsync(GetQuizDetailRequestDto request);
        Task<GetOrganizerQuizzesResponseDto> GetOrganizerQuizzesAsync(GetOrganizerQuizzesRequestDto request);
        Task<GetQuizScoresResponseDto> GetQuizScoresAsync(GetQuizScoresRequestDto request);
        Task<GetStudentQuizScoreResponseDto?> GetStudentQuizScoreAsync(GetStudentQuizScoreRequestDto request);
        Task<PreviewQuizResponseDto> PreviewQuizAsync(PreviewQuizRequestDto request);
        Task<PublishQuizResponseDto> PublishQuizAsync(PublishQuizRequestDto request);
        Task<PublishQuizSetResponseDto> PublishQuizSetAsync(PublishQuizSetRequestDto request);
        Task<CloseQuizResponseDto> CloseQuizAsync(CloseQuizRequestDto request);
        Task<DeleteQuizResponseDto> DeleteQuizAsync(DeleteQuizRequestDto request);
        Task<UpdateQuizQuestionResponseDto> UpdateQuizQuestionAsync(UpdateQuizQuestionRequestDto request);
        Task<DeleteQuizQuestionResponseDto> DeleteQuizQuestionAsync(DeleteQuizQuestionRequestDto request);
        Task<UpdateQuizSetResponseDto> UpdateQuizSetAsync(UpdateQuizSetRequestDto request);
        Task<UploadQuizFileResponseDto> UploadQuizFileAsync(UploadQuizFileRequestDto request);
    }
}
