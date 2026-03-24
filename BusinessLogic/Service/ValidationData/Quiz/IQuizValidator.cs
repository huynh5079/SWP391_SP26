using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UpdateQuiz;

namespace BusinessLogic.Service.ValidationData.Quiz
{
	public interface IQuizValidator
	{
		void ValidateAddQuizSet(CreateQuizSetRequestDto quiz);
		void ValidateAddQuestion(AddQuizQuestionRequestDto question);
		void ValidateUpdateQuizSet(UpdateQuizSetRequestDto quiz);
		void ValidateUpdateQuizQuestion(QuizQuestionContract dto);
		void ValidatePassingScorewithQuestion(int? passingScore, IEnumerable<QuizQuestionContract> questions);
		void ValidateCheckDuplicateQuestion(IEnumerable<QuizQuestionContract> questions);
		void ValidateQuestionCount(IEnumerable<QuizQuestionContract> questions);
		void ValidatePublishQuiz(DataAccess.Entities.EventQuiz quiz);
	}
}
