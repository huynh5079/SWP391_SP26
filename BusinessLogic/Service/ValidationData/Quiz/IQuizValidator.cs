using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz;
using BusinessLogic.DTOs.Event.Quiz;
using DataAccess.Enum;

namespace BusinessLogic.Service.ValidationData.Quiz
{
	public interface IQuizValidator
	{
	//Add
		public void ValidateAddQuizSet(QuizDTO quiz);
		public void ValidateAddQuestion(QuizQuestionDTO question);
	//Update
	   public void ValidateUpdateQuizSet(QuizDTO quiz);
	    public void ValidateUpdateQuizQuestion(QuizQuestionDTO dto);
		public void ValidatePassingScorewithQuestion(QuizDTO dto);
		public void ValidateCheckDuplicateQuestion(string quizsetId, QuizDTO dto);
        public void ValidateQuestionCount(string quizsetId, QuizDTO dto);
		
	}
}
