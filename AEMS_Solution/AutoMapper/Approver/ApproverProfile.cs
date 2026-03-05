using System;
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
            CreateMap<EventItemDto, ApproverEventCardVm>()
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime ?? DateTime.MinValue));

            CreateMap<ApproverEventDetailDto, ApproverEventDetailVm>();
            CreateMap<ApprovalLogDto, ApprovalLogVm>();
        }
    }
}
