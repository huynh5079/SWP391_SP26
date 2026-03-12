using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event.EventQuiz;
using AutoMapper;
using BusinessLogic.DTOs.Event.Quiz.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.GetQuiz;
using BusinessLogic.DTOs.Event.Quiz.GetQuizScores;
using BusinessLogic.DTOs.Event.Quiz.UploadQuizFile;
using BusinessLogic.Service.Event.Sub_Service.Quiz;
using BusinessLogic.Service.Event.Sub_Service.Topic;
using BusinessLogic.Service.Organizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
                var userId = EnsureCurrentUserId();
                var organizerEvents = await _organizerService.GetMyEventsAsync(userId);
                vm.EventId = eventId ?? string.Empty;

                var eventItems = organizerEvents
                    .Where(x => x.HasQuiz && !string.IsNullOrWhiteSpace(x.QuizId))
                    .Where(x => string.IsNullOrWhiteSpace(eventId) || x.EventId == eventId)
                    .ToList();

                foreach (var item in eventItems)
                {
                    var detail = await _quizService.GetQuizDetailAsync(new GetQuizDetailRequestDto
                    {
                        QuizId = item.QuizId!
                    });

                    if (detail != null)
                    {
                        vm.Quizzes.Add(detail.Quiz);
                    }
                }
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return View("~/Views/Event/EventQuiz/CreateQuiz/Index.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? eventId, string? mode)
        {
            mode = NormalizeCreateMode(mode);
            var vm = new EventQuizViewModel
            {
                EventId = eventId ?? string.Empty
            };
            EnsureManualQuestions(vm);
            await LoadDropdowns(vm);
            return View(GetCreateViewPath(mode), vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventQuizViewModel vm, string? mode)
        {
            mode = NormalizeCreateMode(mode);
            EnsureManualQuestions(vm);

            if (mode == "manual")
            {
                ValidateManualQuestions(vm);
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(vm);
                return View(GetCreateViewPath(mode), vm);
            }

            try
            {
                var userId = EnsureCurrentUserId();
                var request = new CreateQuizSetRequestDto
                {
                    UserId = userId,
                    EventId = vm.EventId,
                    TopicId = string.IsNullOrWhiteSpace(vm.TopicId) ? null : vm.TopicId,
                    Title = vm.Quiz?.Title ?? string.Empty,
                    Type = vm.Quiz?.Type ?? default,
                    PassingScore = vm.Quiz?.PassingScore,
                    FileQuiz = vm.Quiz?.FileQuiz,
                    LiveQuizLink = vm.Quiz?.Type == DataAccess.Enum.QuizTypeEnum.LiveQuiz ? vm.Quiz.LiveQuizLink : null
                };

                var created = await _quizService.CreateQuizSetAsync(request);

                if (mode == "manual")
                {
                    foreach (var question in GetSubmittedManualQuestions(vm))
                    {
                        question.QuizId = created.Quiz.EventQuizId;
                        question.CorrectAnswer = NormalizeCorrectAnswer(question.CorrectAnswer, question.TypeOption);
                        await _quizService.AddQuizQuestionAsync(question);
                    }
                }

                if (vm.FileUpload != null && vm.FileUpload.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await vm.FileUpload.CopyToAsync(ms);
                    await _quizService.UploadQuizFileAsync(new UploadQuizFileRequestDto
                    {
                        UserId = userId,
                        QuizId = created.Quiz.EventQuizId,
                        FileContent = ms.ToArray(),
                        FileName = vm.FileUpload.FileName
                    });
                }

                SetSuccess("Tạo quiz thành công.");
                return RedirectToAction(nameof(Details), new { quizId = created.Quiz.EventQuizId });
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
            return View(GetCreateViewPath(mode), vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string quizId)
        {
            var vm = new EventQuizViewModel();
            try
            {
                var userId = EnsureCurrentUserId();
                var organizerEvents = await _organizerService.GetMyEventsAsync(userId);
                var ownerEvent = organizerEvents.FirstOrDefault(x => x.QuizId == quizId);
                if (ownerEvent == null)
                {
                    throw new InvalidOperationException("Không tìm thấy quiz thuộc organizer hiện tại.");
                }

                var detail = await _quizService.GetQuizDetailAsync(new GetQuizDetailRequestDto
                {
                    QuizId = quizId
                });
                if (detail == null)
                {
                    throw new InvalidOperationException("Quiz không tồn tại.");
                }

                var scores = await _quizService.GetQuizScoresAsync(new GetQuizScoresRequestDto
                {
                    QuizId = quizId
                });

                vm.Detail = detail;
                vm.Quiz = detail.Quiz;
                vm.Questions = detail.Questions;
                vm.Scores = scores.Scores;
                vm.EventId = detail.Quiz.EventId;
                vm.TopicId = detail.Quiz.TopicId ?? string.Empty;
                vm.EventTitle = ownerEvent.Title;
                vm.TopicName = ownerEvent.TopicName ?? string.Empty;
                vm.NewQuestion.QuizId = quizId;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Event/EventQuiz/CreateQuiz/Details.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(string quizId, EventQuizViewModel vm)
        {
            try
            {
                vm.NewQuestion.QuizId = quizId;
                await _quizService.AddQuizQuestionAsync(vm.NewQuestion);
                SetSuccess("Đã thêm câu hỏi vào quiz.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToAction(nameof(Details), new { quizId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(string quizId, EventQuizViewModel vm)
        {
            try
            {
                var userId = EnsureCurrentUserId();
                if (vm.FileUpload == null || vm.FileUpload.Length == 0)
                {
                    throw new InvalidOperationException("Vui lòng chọn file quiz để upload.");
                }

                using var ms = new MemoryStream();
                await vm.FileUpload.CopyToAsync(ms);
                await _quizService.UploadQuizFileAsync(new UploadQuizFileRequestDto
                {
                    UserId = userId,
                    QuizId = quizId,
                    FileContent = ms.ToArray(),
                    FileName = vm.FileUpload.FileName
                });

                SetSuccess("Đã upload file quiz thành công.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToAction(nameof(Details), new { quizId });
        }

        private async Task LoadDropdowns(EventQuizViewModel vm)
        {
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
            }
        }

        private string EnsureCurrentUserId()
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                throw new InvalidOperationException("Không xác định được organizer hiện tại.");
            }

            return CurrentUserId;
        }

        private static string NormalizeCreateMode(string? mode)
        {
            return string.Equals(mode, "upload", StringComparison.OrdinalIgnoreCase)
                ? "upload"
                : "manual";
        }

        private static string GetCreateViewPath(string mode)
        {
            return string.Equals(mode, "upload", StringComparison.OrdinalIgnoreCase)
                ? "~/Views/Event/EventQuiz/CreateQuiz/Uploadfile/Create.cshtml"
                : "~/Views/Event/EventQuiz/CreateQuiz/Manual/Create.cshtml";
        }

        private static void EnsureManualQuestions(EventQuizViewModel vm)
        {
            vm.ManualQuestions ??= new List<AddQuizQuestionRequestDto>();

            if (vm.ManualQuestions.Count == 0)
            {
                vm.ManualQuestions.Add(new AddQuizQuestionRequestDto());
            }
        }

        private void ValidateManualQuestions(EventQuizViewModel vm)
        {
            var questions = GetSubmittedManualQuestions(vm);

            if (!questions.Any())
            {
                ModelState.AddModelError(string.Empty, "Vui lòng thêm ít nhất 1 câu hỏi cho quiz tạo thủ công.");
                return;
            }

            for (var i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                if (string.IsNullOrWhiteSpace(question.QuestionText)
                    || (question.TypeOption == DataAccess.Enum.QuestionTypeOptionEnum.TrueFalse && string.IsNullOrWhiteSpace(question.CorrectAnswer)))
                {
                    ModelState.AddModelError(string.Empty, $"Câu hỏi {i + 1} cần nhập nội dung câu hỏi.");
                }

                if (question.TypeOption != DataAccess.Enum.QuestionTypeOptionEnum.TrueFalse
                    && (string.IsNullOrWhiteSpace(question.Options.OptionA)
                        || string.IsNullOrWhiteSpace(question.Options.OptionB)
                        || string.IsNullOrWhiteSpace(question.Options.OptionC)
                        || string.IsNullOrWhiteSpace(question.Options.OptionD)))
                {
                    ModelState.AddModelError(string.Empty, $"Câu hỏi {i + 1} cần nhập đủ 4 đáp án.");
                }

                var correctAnswer = NormalizeCorrectAnswer(question.CorrectAnswer, question.TypeOption);
                if (question.TypeOption == DataAccess.Enum.QuestionTypeOptionEnum.MultipleChoice)
                {
                    if (string.IsNullOrWhiteSpace(correctAnswer))
                    {
                        ModelState.AddModelError(string.Empty, $"Câu hỏi {i + 1} phải chọn ít nhất 1 đáp án đúng.");
                    }
                }
                else if (question.TypeOption == DataAccess.Enum.QuestionTypeOptionEnum.TrueFalse)
                {
                    if (correctAnswer != "A" && correctAnswer != "B")
                    {
                        ModelState.AddModelError(string.Empty, $"Câu hỏi {i + 1} dạng True/False chỉ được chọn True hoặc False.");
                    }
                }
                else if (correctAnswer != "A" && correctAnswer != "B" && correctAnswer != "C" && correctAnswer != "D")
                {
                    ModelState.AddModelError(string.Empty, $"Câu hỏi {i + 1} phải chọn đáp án đúng là A, B, C hoặc D.");
                }

                if (question.ScorePoint <= 0)
                {
                    question.ScorePoint = 1;
                }
            }
        }

        private static List<AddQuizQuestionRequestDto> GetSubmittedManualQuestions(EventQuizViewModel vm)
        {
            return (vm.ManualQuestions ?? new List<AddQuizQuestionRequestDto>())
                .Where(q => !string.IsNullOrWhiteSpace(q.QuestionText)
                    || !string.IsNullOrWhiteSpace(q.Options.OptionA)
                    || !string.IsNullOrWhiteSpace(q.Options.OptionB)
                    || !string.IsNullOrWhiteSpace(q.Options.OptionC)
                    || !string.IsNullOrWhiteSpace(q.Options.OptionD)
                    || !string.IsNullOrWhiteSpace(q.CorrectAnswer))
                .ToList();
        }

        private static string NormalizeCorrectAnswer(string? correctAnswer, DataAccess.Enum.QuestionTypeOptionEnum typeOption)
        {
            if (string.IsNullOrWhiteSpace(correctAnswer))
            {
                return string.Empty;
            }

            var answers = System.Text.RegularExpressions.Regex.Matches(correctAnswer.ToUpperInvariant(), "[ABCD]")
                .Select(m => m.Value)
                .Distinct()
                .ToList();

            if (typeOption == DataAccess.Enum.QuestionTypeOptionEnum.MultipleChoice)
            {
                return string.Join(",", answers);
            }

            return answers.FirstOrDefault() ?? string.Empty;
        }
    }
}
