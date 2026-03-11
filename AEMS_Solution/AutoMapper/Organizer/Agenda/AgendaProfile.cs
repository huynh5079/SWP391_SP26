using AEMS_Solution.Models.Event.EventAgenda;
using AutoMapper;

namespace AEMS_Solution.AutoMapper.Organizer.Agenda
{
	public class AgendaProfile : Profile
	{
		public AgendaProfile()
		{
			CreateMap<DataAccess.Entities.EventAgenda, AgendaItemViewModel>()
				.ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : string.Empty))
				.ForMember(dest => dest.OrganizerName, opt => opt.MapFrom(src => src.Event != null && src.Event.Organizer != null && src.Event.Organizer.User != null ? src.Event.Organizer.User.FullName : string.Empty))
				.ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.Event != null ? src.Event.Type : null))
				.ForMember(dest => dest.EventStatus, opt => opt.MapFrom(src => src.Event != null ? src.Event.Status : default));

			CreateMap<DataAccess.Entities.EventAgenda, EditAgendaViewModel>()
				.ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event != null ? src.Event.Title : string.Empty));

			CreateMap<EditAgendaViewModel, DataAccess.Entities.EventAgenda>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.EventId, opt => opt.Ignore())
				.ForMember(dest => dest.Event, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
		}
	}
}
