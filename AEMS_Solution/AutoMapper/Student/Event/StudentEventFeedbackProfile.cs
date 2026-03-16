using AEMS_Solution.Models.Event.Feedback.ForStudentFeedback;
using AutoMapper;
using BusinessLogic.DTOs.Student;

namespace AEMS_Solution.AutoMapper.Student.Event
{
	public class StudentEventFeedbackProfile : Profile
	{
		public StudentEventFeedbackProfile()
		{
			CreateMap<StudentEventDetailDto, StudentEventFeedbackViewModel>()
				.ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
				.ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Title))
				.ForMember(dest => dest.Rating, opt => opt.MapFrom(_ => 5));

			CreateMap<StudentEventFeedbackViewModel, SubmitFeedbackRequestDto>();
		}
	}
}
