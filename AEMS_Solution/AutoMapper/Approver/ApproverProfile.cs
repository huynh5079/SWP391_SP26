using System;
using System.Linq;
using AEMS_Solution.Models.Approver;
using AutoMapper;
using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.Role.Organizer;

namespace AEMS_Solution.AutoMapper.Approver
{
    public class ApproverProfile : Profile
    {
        public ApproverProfile()
        {
            //Manage for Approver
            CreateMap<EventItemDto, ApproverEventCardVm>()
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime ?? DateTime.MinValue))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.OrganizerId, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizerName, opt => opt.Ignore())
                .ForMember(dest => dest.TimeState, opt => opt.Ignore())
                .ForMember(dest => dest.LastApprovalComment, opt => opt.MapFrom(src => src.LastApprovalComment));

            CreateMap<ApprovalLogDto, ApprovalLogVm>().ReverseMap();

            CreateMap<ApproverEventDetailDto, ApproverEventDetailVm>().ReverseMap();

            CreateMap<ApproverDto, ApproverEventCardVm>()
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Event.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Event.Title))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.Event.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.Event.EndTime ?? DateTime.MinValue))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Event.Status))
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.Event.ThumbnailUrl))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Event.Location))
                .ForMember(dest => dest.LastApprovalComment, opt => opt.MapFrom(src => src.ApprovalLogs.OrderByDescending(x => x.CreatedAt).Select(x => x.Comment).FirstOrDefault()));

            CreateMap<ApproverDto, ApproverEventDetailVm>()
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Event.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Event.Title))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.Event.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.Event.EndTime ?? DateTime.MinValue))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Event.Status))
                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.MaxCapacity, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizerName, opt => opt.MapFrom(src => src.Event.OrganizerName ?? string.Empty))
                .ForMember(dest => dest.OrganizerEmail, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovalLogs, opt => opt.MapFrom(src => src.ApprovalLogs));

            CreateMap<ApproverEventDetailDto, ApproverActionFormVm>()
                .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Heading, opt => opt.Ignore())
                .ForMember(dest => dest.Operation, opt => opt.Ignore());

        }
    }
}
