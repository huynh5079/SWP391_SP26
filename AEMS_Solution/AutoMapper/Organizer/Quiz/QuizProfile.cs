using AutoMapper;
using AEMS_Solution.Models.Event.EventQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuizScores;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UpdateQuiz;
using DataAccess.Enum;

namespace AEMS_Solution.AutoMapper.Organizer.Quiz
{
    public class QuizProfile : Profile
    {
        public QuizProfile()
        {
			CreateMap<QuizSummaryContract, EventQuizViewModel>()
                .ForMember(dest => dest.Quiz, opt => opt.MapFrom(src => src));

			// Edit quiz setup popup posts vm.Quiz (QuizSummaryContract) -> UpdateQuizSetRequestDto
			CreateMap<QuizSummaryContract, UpdateQuizSetRequestDto>()
				.ForMember(dest => dest.UserId, opt => opt.Ignore())
				.ForMember(dest => dest.QuizId, opt => opt.Ignore())
				.ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
				.ForMember(dest => dest.TopicId, opt => opt.MapFrom(src => src.TopicId))
				.ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
				.ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
				.ForMember(dest => dest.PassingScore, opt => opt.MapFrom(src => src.PassingScore))
				.ForMember(dest => dest.TimeLimit, opt => opt.MapFrom(src => src.TimeLimit))
				.ForMember(dest => dest.QuizStartTime, opt => opt.MapFrom(src => src.QuizStartTime))
				.ForMember(dest => dest.QuizEndTime, opt => opt.MapFrom(src => src.QuizEndTime))
				.ForMember(dest => dest.FileQuiz, opt => opt.MapFrom(src => src.FileQuiz))
				.ForMember(dest => dest.LiveQuizLink, opt => opt.MapFrom(src => src.LiveQuizLink))
				.ForMember(dest => dest.AllowReview, opt => opt.MapFrom(src => src.AllowReview))
				.ForMember(dest => dest.MaxAttempts, opt => opt.MapFrom(src => src.MaxAttempts));

			CreateMap<GetQuizDetailResponseDto, EventQuizViewModel>()
				.ForMember(dest => dest.Detail, opt => opt.MapFrom(src => src))
				.ForMember(dest => dest.Quiz, opt => opt.MapFrom(src => src.Quiz))
				.ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions))
				.ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Quiz.EventId))
				.ForMember(dest => dest.TopicId, opt => opt.MapFrom(src => src.Quiz.TopicId ?? string.Empty));

			CreateMap<GetQuizScoresResponseDto, EventQuizViewModel>()
				.ForMember(dest => dest.Scores, opt => opt.MapFrom(src => src.Scores));

			CreateMap<EventQuizViewModel, CreateQuizSetRequestDto>()
				.ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
				.ForMember(dest => dest.SourceQuizSetId, opt => opt.MapFrom(src => src.SelectedQuizSetId))
				.ForMember(dest => dest.TopicId, opt => opt.MapFrom(src => src.TopicId))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Title : string.Empty))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Type : default))
                .ForMember(dest => dest.PassingScore, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.PassingScore : null))
				.ForMember(dest => dest.TimeLimit, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.TimeLimit : null))
				.ForMember(dest => dest.QuizStartTime, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.QuizStartTime : null))
				.ForMember(dest => dest.QuizEndTime, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.QuizEndTime : null))
				.ForMember(dest => dest.FileQuiz, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.FileQuiz : null))
				.ForMember(dest => dest.LiveQuizLink, opt => opt.MapFrom(src => src.Quiz != null && src.Quiz.Type == QuizTypeEnum.LiveQuiz ? src.Quiz.LiveQuizLink : null))
				.ForMember(dest => dest.AllowReview, opt => opt.MapFrom(src => src.Quiz != null && src.Quiz.AllowReview))
				.ForMember(dest => dest.SharingStatus, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.SharingStatus : QuizSetVisibilityEnum.Private));
        }
    }
}
