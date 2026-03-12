using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event.EventQuiz;
using AutoMapper;
using BusinessLogic.Service.Event.Sub_Service.Quiz;
using BusinessLogic.Service.Event.Sub_Service.Topic;
using BusinessLogic.Service.Organizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.IO;

namespace AEMS_Solution.Controllers.Event.EventQuiz
{
    [Authorize(Roles = "Organizer")]
    public class EventQuizController : BaseController
    {
        private readonly IQuizService _quizService;
        private readonly ITopicService _topicService;
        private readonly IOrganizerService _organizerService;
        private readonly IMapper _mapper;

        public EventQuizController(IQuizService quizService, ITopicService topicService, IOrganizerService organizerService, IMapper mapper)
        {
            _quizService = quizService;
            _topicService = topicService;
            _organizerService = organizerService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? eventId)
        {
            var vm = new EventQuizViewModel();
            try
            {
                var quizzes = string.IsNullOrWhiteSpace(eventId);
                    //? await _quizService.GetAllAsync()
                   // : await _quizService.GetByEventIdAsync(eventId);

                //vm.Quizzes = quizzes;
                vm.EventId = eventId ?? string.Empty;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return View("~/Views/Event/EventQuiz/Index.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new EventQuizViewModel();
            await LoadDropdowns(vm);
            return View("~/Views/Event/EventQuiz/Create.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventQuizViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(vm);
                return View("~/Views/Event/EventQuiz/Create.cshtml", vm);
            }

            try
            {
                var dto = _mapper.Map<BusinessLogic.DTOs.Event.Quiz.QuizDTO>(vm);

                var created = await _quizService.AddQuizSetAsync(dto);

                // upload file if provided
                if (vm.FileUpload != null && vm.FileUpload.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await vm.FileUpload.CopyToAsync(ms);
                    await _quizService.UploadFileAsync(created.QuizsetId, ms.ToArray(), vm.FileUpload.FileName);
                }

                SetSuccess("Tạo quiz thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            await LoadDropdowns(vm);
            return View("~/Views/Event/EventQuiz/Create.cshtml", vm);
        }

        private async Task LoadDropdowns(EventQuizViewModel vm)
        {
            // events for the organizer
            try
            {
                var userId = CurrentUserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    var events = await _organizerService.GetMyEventsAsync(userId);
                    vm.Events = events.Select(e => new SelectListItem(e.Title, e.EventId)).ToList();
                }

                var topics = await _topicService.GetAllTopicsAsync();
                vm.Topics = topics.Select(t => new SelectListItem(t.TopicName, t.TopicId)).ToList();
            }
            catch
            {
                // ignore dropdown load errors for now
            }
        }
    }
}
