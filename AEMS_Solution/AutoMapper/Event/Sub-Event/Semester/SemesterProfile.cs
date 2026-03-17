using AEMS_Solution.Models.Event.Semester;
using AutoMapper;
using BusinessLogic.DTOs.Event.Semester;

namespace AEMS_Solution.AutoMapper.Event.Sub_Event.Semester
{
	public class SemesterProfile : Profile
	{
		public SemesterProfile()
		{
			CreateMap<SemesterDTO, SemesterItemViewModel>()
              .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src => src.SemesterId ?? string.Empty))
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
				.ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code ?? string.Empty))
				.ForMember(dest => dest.EventCount, opt => opt.MapFrom(src => src.EventList != null ? src.EventList.Count : 0));
		}
	}
}
