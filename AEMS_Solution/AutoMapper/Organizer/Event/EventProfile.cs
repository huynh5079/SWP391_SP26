using AEMS_Solution.Models.Event;
using AutoMapper;
using BusinessLogic.DTOs.Role.Organizer;
using Microsoft.AspNetCore.Mvc.Rendering;
using AEMS_Solution.Models.Organizer;

namespace AEMS_Solution.AutoMapper.Organizer.Event
{
	
	public class EventProfile : Profile
	{
		public EventProfile()
		{
			//Mapper create
			CreateMap<CreateEventViewModel, CreateEventRequestDto>()
				.ForMember(dest => dest.Capacity,
						   opt => opt.MapFrom(src => src.MaxCapacity))
				.ForMember(dest => dest.BannerUrl,
						   opt => opt.MapFrom(src => src.ThumbnailUrl));

			CreateMap<CreateAgendaItemVm, CreateAgendaItemDto>();
			CreateMap<CreateDocumentVm, CreateDocumentDto>();
			CreateMap<UpdateEventViewModel, UpdateEventRequestDto>()
				.ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.MaxCapacity))
				.ForMember(dest => dest.BannerUrl, opt => opt.MapFrom(src => src.ThumbnailUrl));
			CreateMap<UpdateAgendaItemVm, UpdateAgendaItemDto>();
			CreateMap<UpdateDocumentVm, UpdateDocumentDto>();
			CreateMap<SelectItemDto, SelectListItem>()
				.ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Id))
				.ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text));

			CreateMap<CreateEventDropdownsDto, CreateEventViewModel>()
				.ForMember(dest => dest.Semesters, opt => opt.MapFrom(src => src.Semesters))
				.ForMember(dest => dest.Departments, opt => opt.MapFrom(src => src.Departments))
				.ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.Locations))
				.ForMember(dest => dest.Topics, opt => opt.MapFrom(src => src.Topics));
			CreateMap<CreateEventDropdownsDto, UpdateEventViewModel>()
				.ForMember(dest => dest.Semesters, opt => opt.MapFrom(src => src.Semesters))
				.ForMember(dest => dest.Departments, opt => opt.MapFrom(src => src.Departments))
				.ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.Locations))
				.ForMember(dest => dest.Topics, opt => opt.MapFrom(src => src.Topics));

			// Detail mappings
			CreateMap<EventDetailsDto, EventDetailsViewModel>()
				.ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location))
				.ForMember(dest => dest.LocationText, opt => opt.MapFrom(src => src.Location))
				.ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.HasValue ? src.Type.Value.ToString() : null));
			CreateMap<EventDetailsDto, UpdateEventViewModel>();
			CreateMap<EventAgendaDto, EventAgendaVm>();
			CreateMap<EventAgendaDto, UpdateAgendaItemVm>();
			CreateMap<EventDocumentDto, EventDocumentVm>()
				.ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName ?? src.Url ?? string.Empty))
				.ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url ?? string.Empty));
			CreateMap<EventDocumentDto, UpdateDocumentVm>()
				.ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName ?? src.Url ?? string.Empty))
				.ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url ?? string.Empty));
			
			CreateMap<EventTeamDto, EventTeamVm>();
			CreateMap<TeamMemberDto, TeamMemberVm>();
			  
			// List card mappings
			CreateMap<EventListDto, OrganizerEventCardVm>();
			CreateMap<EventItemDto, OrganizerEventCardVm>()
				.ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id));

			// Dashboard mappings
			CreateMap<OrganizerDto, OrganizerDashboardStats>()
				.ForMember(dest => dest.TotalEvents, opt => opt.MapFrom(src => src.TotalEvents))
				.ForMember(dest => dest.UpcomingEvents, opt => opt.MapFrom(src => src.UpcomingEvents))
				.ForMember(dest => dest.DraftEvents, opt => opt.MapFrom(src => src.DraftEvents));

			CreateMap<OrganizerDto, OrganizerDashboardViewModel>()
				.ForMember(dest => dest.Stats, opt => opt.MapFrom(src => src))
				.ForMember(dest => dest.RecentEvents, opt => opt.MapFrom(src => src.UpcomingList))
				.ForMember(dest => dest.RecentFeedbacks, opt => opt.MapFrom(src => src.RecentFeedbacks));
			CreateMap<EventFeedbackSummaryDto, EventFeedbackSummaryVm>();
	}

	}
}
