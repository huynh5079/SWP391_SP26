using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz.QuizForAll;

namespace BusinessLogic.Service.Event.Sub_Service.Quiz.ForAll
{
	public interface IQuizServiceForAll
	{
		Task<GetCurrentQuizSessionResponseDto> GetCurrentQuizSessionAsync(GetCurrentQuizSessionRequestDto request);
		Task<StartQuizResponseDto> StartQuizAsync(StartQuizRequestDto request);
		Task<SubmitQuizResponseDto> SubmitQuizAsync(SubmitQuizRequestDto request);
	}
}
