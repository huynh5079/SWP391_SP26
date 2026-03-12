using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.GetQuiz;
using BusinessLogic.DTOs.Event.Quiz.GetQuizScores;
using BusinessLogic.DTOs.Event.Quiz.UpdateQuiz;
using BusinessLogic.DTOs.Event.Quiz.UploadQuizFile;

namespace BusinessLogic.Service.Event.Sub_Service.Quiz
{
    public interface IQuizService
    {
        Task<CreateQuizSetResponseDto> CreateQuizSetAsync(CreateQuizSetRequestDto request);
        Task<AddQuizQuestionResponseDto> AddQuizQuestionAsync(AddQuizQuestionRequestDto request);
        Task<GetQuizDetailResponseDto?> GetQuizDetailAsync(GetQuizDetailRequestDto request);
        Task<GetQuizScoresResponseDto> GetQuizScoresAsync(GetQuizScoresRequestDto request);
        Task<GetStudentQuizScoreResponseDto?> GetStudentQuizScoreAsync(GetStudentQuizScoreRequestDto request);
        Task<UpdateQuizSetResponseDto> UpdateQuizSetAsync(UpdateQuizSetRequestDto request);
        Task<UploadQuizFileResponseDto> UploadQuizFileAsync(UploadQuizFileRequestDto request);
    }
}
