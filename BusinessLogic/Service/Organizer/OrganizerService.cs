using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.ValiDate.ValidationDataforEvent;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using Microsoft.EntityFrameworkCore;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Dashboard;

namespace BusinessLogic.Service.Organizer
{
    // Keep OrganizerService as a thin facade for backward compatibility if needed
    public class OrganizerService : IOrganizerService
    {
        private readonly IEventService _eventService;
        private readonly IDropdownService _dropdownService;
        private readonly IDashboardService _dashboardService;
		private readonly IEventWaitlistService _eventwaitlist;
		public OrganizerService(IEventService eventService, IDropdownService dropdownService, IDashboardService dashboardService, IEventWaitlistService eventwaitlist)
        {
            _eventService = eventService;
            _dropdownService = dropdownService;
            _dashboardService = dashboardService;
			_eventwaitlist = eventwaitlist;
        }

		public async Task<BusinessLogic.DTOs.Role.Organizer.PagedResult<EventListDto>> GetMyEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
		{
			return await _eventService.GetMyEventsAsync(userId, search, status, semesterId, page, pageSize);
		}
		//my events: lấy tất cả event của organizer, kèm theo các thông tin cần thiết để hiển thị ở list (thumbnail, time, location, status, registered count, checked-in count, waitlist count, avg rating)
		public async Task<List<EventListDto>> GetMyEventsAsync(string userId)
		{
			return await _eventService.GetMyEventsAsync(userId);
		}

		// ======= DASHBOARD =======
		public async Task<OrganizerDto> GetDashboardAsync(string userId)
		{
			return await _dashboardService.GetDashboardAsync(userId);
		}

		// ======= COUNTERS =======
		public Task<int> GetTotalEventAsync(string userId) => _dashboardService.GetTotalEventAsync(userId);

		public Task<int> GetUpcomingEventAsync(string userId) => _dashboardService.GetUpcomingEventAsync(userId);

		public Task<int> GetDraftEventAsync(string userId) => _dashboardService.GetDraftEventAsync(userId);

		// ======= CRUD EVENT =======
		public Task<string> CreateEventAsync(string userId, CreateEventRequestDto dto) => _eventService.CreateEventAsync(userId, dto);

		public Task UpdateEventAsync(string userId, string eventId, UpdateEventRequestDto dto) => _eventService.UpdateEventAsync(userId, eventId, dto);

		public Task DeleteEventAsync(string userId, string eventId) => _eventService.DeleteEventAsync(userId, eventId);

        public Task SoftDeleteEventAsync(string userId, string eventId) => _eventService.SoftDeleteEventAsync(userId, eventId);

        public Task RestoreEventAsync(string userId, string eventId) => _eventService.RestoreEventAsync(userId, eventId);

        public Task<PagedResult<EventListDto>> GetMyDeletedEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
            => _eventService.GetMyDeletedEventsAsync(userId, search, status, semesterId, page, pageSize);

        public Task SendForApprovalAsync(string userId, string eventId) => _eventService.SendForApprovalAsync(userId, eventId);

        public Task CancelEventAsync(string userId, string eventId) => _eventService.CancelEventAsync(userId, eventId);

        public Task PublishEventAsync(string userId, string eventId) => _eventService.PublishEventAsync(userId, eventId);

        public Task<EventDetailsDto> GetEventDetailsAsync(string eventId, string? userId = null) => _eventService.GetEventDetailsAsync(eventId, userId);

		public Task<CreateEventDropdownsDto> GetCreateEventDropdownsAsync() => _dropdownService.GetCreateEventDropdownsAsync();


        // ======= EVENT WaitList =======

		public Task AddToWaitlistAsync(AddToWaitlistRequestDto dto)=>_eventwaitlist.AddToWaitlistAsync(dto);

		public Task RemoveFromWaitlistAsync(string studentId, string eventId)=>_eventwaitlist.RemoveFromWaitlistAsync(studentId, eventId);

		public Task<List<EventWaitlistDto>> GetWaitlistByEventAsync(string eventId)=>_eventwaitlist.GetWaitlistByEventAsync(eventId);

		public Task OfferNextAsync(string eventId)=>_eventwaitlist.OfferNextAsync(eventId);
		public Task RespondToOfferAsync(RespondOfferRequestDto dto)=>_eventwaitlist.RespondToOfferAsync(dto);

	}
}