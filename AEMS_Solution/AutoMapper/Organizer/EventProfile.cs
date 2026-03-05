using AEMS_Solution.Models.Event;
using AutoMapper;
using BusinessLogic.DTOs.Role.Organizer;
namespace AEMS_Solution.AutoMapper.Organizer
{
	using AEMS_Solution.Models.Event;
	using AEMS_Solution.Models.Organizer;
	using AutoMapper;
	using BusinessLogic.DTOs.Role.Organizer;
	public class EventProfile : Profile
	{
		public EventProfile()
		{
			CreateMap<CreateEventViewModel, CreateEventRequestDto>()
				.ForMember(dest => dest.Capacity,
						   opt => opt.MapFrom(src => src.MaxCapacity))
				.ForMember(dest => dest.BannerUrl,
						   opt => opt.MapFrom(src => src.ThumbnailUrl));

			CreateMap<CreateAgendaItemVm, CreateAgendaItemDto>();

			// Detail mappings
			CreateMap<EventDetailsDto, EventDetailsViewModel>();
			CreateMap<EventAgendaDto, EventAgendaVm>();
			CreateMap<EventDocumentDto, EventDocumentVm>();

			// List card mappings
			CreateMap<EventListDto, OrganizerEventCardVm>();
			CreateMap<EventItemDto, OrganizerEventCardVm>()
				.ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id));

			// Dashboard mappings
			CreateMap<OrganizerDto, OrganizerDashboardViewModel>();
			CreateMap<EventFeedbackSummaryDto, EventFeedbackSummaryVm>();
	}
	}
}
