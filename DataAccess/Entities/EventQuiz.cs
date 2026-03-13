using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace DataAccess.Entities;
//list of question
public partial class EventQuiz : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string? QuizSetId { get; set; }

    public int? TimeLimit { get; set; }

    public string Title { get; set; } = null!;

    public QuizTypeEnum Type { get; set; }

    public bool IsActive { get; set; }

    public int? PassingScore { get; set; }
    //tập câu hỏi còn khả dụng hay ko??
    public QuestionSetEnum QuestionSetStatus { get; set; }
	public QuizStatusEnum Status {get; set; }
	//public DateTime? CreatedAt { get; set; }
	public string? FileQuiz { get; set; }
	public string? LiveQuizLink { get; set; }
	public bool AllowReview { get; set; }
	public virtual Event Event { get; set; } = null!;
	public virtual QuizSet? QuizSet { get; set; }
	public virtual ICollection<EventQuizQuestion> EventQuizQuestions { get; set; } = new List<EventQuizQuestion>();

    public virtual ICollection<StudentQuizScore> StudentQuizScores { get; set; } = new List<StudentQuizScore>();
}
