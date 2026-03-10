using AutoMapper;
using AEMS_Solution.Models.Event.EventQuiz;
using BusinessLogic.DTOs.Event.Quiz;
using System.Collections.Generic;

namespace AEMS_Solution.AutoMapper.Organizer.Quiz
{
    public class QuizProfile : Profile
    {
        public QuizProfile()
        {
            // Map QuizDTO -> EventQuizViewModel (populate the Quiz property)
            CreateMap<QuizDTO, EventQuizViewModel>()
                .ForMember(dest => dest.Quiz, opt => opt.MapFrom(src => src));

            // Map EventQuizViewModel -> QuizDTO (for create/update)
            CreateMap<EventQuizViewModel, QuizDTO>()
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Title : string.Empty))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Type : default))
                .ForMember(dest => dest.PassingScore, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.PassingScore : null))
                .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Questions : new List<QuizQuestionDTO>()));
        }
    }
}
