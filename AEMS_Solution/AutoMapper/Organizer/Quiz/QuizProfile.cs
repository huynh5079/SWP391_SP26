using AutoMapper;
using AEMS_Solution.Models.Event.EventQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;

namespace AEMS_Solution.AutoMapper.Organizer.Quiz
{
    public class QuizProfile : Profile
    {
        public QuizProfile()
        {
			CreateMap<QuizSummaryContract, EventQuizViewModel>()
                .ForMember(dest => dest.Quiz, opt => opt.MapFrom(src => src));

			CreateMap<EventQuizViewModel, CreateQuizSetRequestDto>()
				.ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.TopicId, opt => opt.MapFrom(src => src.TopicId))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Title : string.Empty))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Type : default))
                .ForMember(dest => dest.PassingScore, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.PassingScore : null))
				.ForMember(dest => dest.FileQuiz, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.FileQuiz : null))
				.ForMember(dest => dest.LiveQuizLink, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.LiveQuizLink : null))
				.ForMember(dest => dest.AllowReview, opt => opt.MapFrom(src => src.Quiz != null && src.Quiz.AllowReview));
        }
    }
}
