using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event.EventQuiz;
using AutoMapper;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuizScores;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.QuizActions;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UploadQuizFile;
using BusinessLogic.Service.Event.Sub_Service.Quiz;
using BusinessLogic.Service.Event.Sub_Service.Topic;
using BusinessLogic.Service.Organizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DataAccess.Enum;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public async Task<IActionResult> Index(string? eventId, string? scope)
        {
            var vm = new EventQuizViewModel();
            try
            {
                var userId = EnsureCurrentUserId();
                vm.EventId = eventId ?? string.Empty;
                vm.QuizScope = NormalizeQuizScope(scope);
                var quizzes = await _quizService.GetOrganizerQuizzesAsync(new GetOrganizerQuizzesRequestDto
                {
                    UserId = userId,
                    EventId = vm.EventId
                });

                vm.Quizzes = vm.QuizScope == "community"
                    ? quizzes.Quizzes.Where(x => x.SharingStatus == QuizSetVisibilityEnum.Public).ToList()
                    : quizzes.Quizzes;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return View("~/Views/Event/EventQuiz/CreateQuiz/Index.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> QuestionBank(string? semester, string? eventTitle, string? title)
        {
            var vm = new EventQuizViewModel
            {
                SemesterFilter = semester?.Trim() ?? string.Empty,
                EventTitleFilter = eventTitle?.Trim() ?? string.Empty,
                QuizTitleFilter = title?.Trim() ?? string.Empty
            };

            try
            {
                var userId = EnsureCurrentUserId();
                var quizzes = await _quizService.GetOrganizerQuizzesAsync(new GetOrganizerQuizzesRequestDto
                {
                    UserId = userId
                });

                var filteredQuizzes = quizzes.Quizzes
                    .Where(x => x.QuestionCount > 0)
                    .Where(x => string.IsNullOrWhiteSpace(vm.SemesterFilter)
                        || (!string.IsNullOrWhiteSpace(x.SemesterName)
                            && x.SemesterName.Contains(vm.SemesterFilter, StringComparison.OrdinalIgnoreCase)))
                    .Where(x => string.IsNullOrWhiteSpace(vm.EventTitleFilter)
                        || (!string.IsNullOrWhiteSpace(x.EventTitle)
                            && x.EventTitle.Contains(vm.EventTitleFilter, StringComparison.OrdinalIgnoreCase)))
                    .Where(x => string.IsNullOrWhiteSpace(vm.QuizTitleFilter)
                        || (!string.IsNullOrWhiteSpace(x.Title)
                            && x.Title.Contains(vm.QuizTitleFilter, StringComparison.OrdinalIgnoreCase)))
                    .GroupBy(x => x.QuizSetId)
                    .Select(g => g.OrderByDescending(x => x.UpdatedAt).First())
                    .OrderBy(x => x.SemesterName)
                    .ThenBy(x => x.EventTitle)
                    .ThenBy(x => x.Title)
                    .ToList();

                vm.Quizzes = filteredQuizzes;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return View("~/Views/Event/EventQuiz/CreateQuiz/QuestionBank/Index.cshtml", vm);
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
            else if (mode == "bank" && string.IsNullOrWhiteSpace(vm.TopicId))
            {
                ModelState.AddModelError(nameof(vm.TopicId), "Vui lòng chọn topic trước khi sử dụng question bank.");
            }
            else if (mode == "bank" && string.IsNullOrWhiteSpace(vm.SelectedQuizSetId))
            {
                ModelState.AddModelError(nameof(vm.SelectedQuizSetId), "Vui lòng chọn question bank.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(vm);
                return View(GetCreateViewPath(mode), vm);
            }

            try
            {
                var userId = EnsureCurrentUserId();
                var request = _mapper.Map<CreateQuizSetRequestDto>(vm);
                request.UserId = userId;
                request.SourceQuizSetId = string.IsNullOrWhiteSpace(request.SourceQuizSetId) ? null : request.SourceQuizSetId;
                request.TopicId = string.IsNullOrWhiteSpace(request.TopicId) ? null : request.TopicId;

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
                var detail = await _quizService.GetQuizDetailAsync(new GetQuizDetailRequestDto
                {
                    QuizId = quizId
                });
                if (detail == null)
                {
                    throw new InvalidOperationException("Quiz không tồn tại.");
                }

                var organizerEvents = await _organizerService.GetMyEventsAsync(userId);
                var ownerEvent = organizerEvents.FirstOrDefault(x => x.EventId == detail.Quiz.EventId);
                if (ownerEvent == null)
                {
                    throw new InvalidOperationException("Không tìm thấy quiz thuộc organizer hiện tại.");
                }

                var scores = await _quizService.GetQuizScoresAsync(new GetQuizScoresRequestDto
                {
                    QuizId = quizId
                });

                vm = _mapper.Map<EventQuizViewModel>(detail);
                vm.Scores = _mapper.Map<EventQuizViewModel>(scores).Scores;
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

        [HttpGet]
        public async Task<IActionResult> Preview(string quizId)
        {
            var vm = new EventQuizViewModel();
            try
            {
                var userId = EnsureCurrentUserId();
                var preview = await _quizService.PreviewQuizAsync(new PreviewQuizRequestDto
                {
                    QuizId = quizId,
                    UserId = userId
                });

                if (preview?.Preview == null)
                {
                    throw new InvalidOperationException("Quiz không tồn tại.");
                }

                var organizerEvents = await _organizerService.GetMyEventsAsync(userId);
                var ownerEvent = organizerEvents.FirstOrDefault(x => x.EventId == preview.Preview.Quiz.EventId);
                if (ownerEvent == null)
                {
                    throw new InvalidOperationException("Không tìm thấy quiz thuộc organizer hiện tại.");
                }

                vm = _mapper.Map<EventQuizViewModel>(preview.Preview);
                vm.EventTitle = ownerEvent.Title;
                vm.TopicName = ownerEvent.TopicName ?? string.Empty;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Event/EventQuiz/CreateQuiz/Preview.cshtml", vm);
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
        public async Task<IActionResult> Publish(string quizId)
        {
            try
            {
                var userId = EnsureCurrentUserId();
                await _quizService.PublishQuizAsync(new PublishQuizRequestDto
                {
                    QuizId = quizId,
                    UserId = userId
                });

                SetSuccess("Đã publish quiz thành công.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToReferrerOrIndex();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(string quizId)
        {
            try
            {
                var userId = EnsureCurrentUserId();
                await _quizService.PublishQuizSetAsync(new PublishQuizSetRequestDto
                {
                    QuizId = quizId,
                    UserId = userId,
                    SharingStatus = QuizSetVisibilityEnum.Public
                });

                SetSuccess("Question bank đã được chuyển sang Public và chia sẻ cho community.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToReferrerOrIndex();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string quizId)
        {
            try
            {
                var userId = EnsureCurrentUserId();
                await _quizService.PublishQuizSetAsync(new PublishQuizSetRequestDto
                {
                    QuizId = quizId,
                    UserId = userId,
                    SharingStatus = QuizSetVisibilityEnum.Private
                });

                SetSuccess("Question bank đã được chuyển về Private.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToReferrerOrIndex();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string quizId)
        {
            try
            {
                var userId = EnsureCurrentUserId();
                await _quizService.DeleteQuizAsync(new DeleteQuizRequestDto
                {
                    QuizId = quizId,
                    UserId = userId
                });

                SetSuccess("Đã xóa quiz thành công.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("upload-multiple")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(string quizId, EventQuizViewModel vm)
        {
            try
            {
                var userId = EnsureCurrentUserId();
                if (vm.FileUpload == null || vm.FileUpload.Length == 0)
                {
					SetError("Vui lòng chọn file quiz");
					return RedirectToAction(nameof(Details), new { quizId });
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

                    var quizBanks = await _quizService.GetAvailableQuizBanksAsync(new GetAvailableQuizBanksRequestDto
                    {
                        UserId = userId
                    });
                    vm.AvailableQuizBanks = quizBanks.QuizBanks;
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

        private IActionResult RedirectToReferrerOrIndex()
        {
            var referer = Request.Headers.Referer.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(referer))
            {
                if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)
                    && string.Equals(refererUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return LocalRedirect(refererUri.PathAndQuery);
                }

                if (Url.IsLocalUrl(referer))
                {
                    return LocalRedirect(referer);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private static string NormalizeCreateMode(string? mode)
        {
            if (string.Equals(mode, "bank", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "questionbank", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "question-bank", StringComparison.OrdinalIgnoreCase))
            {
                return "bank";
            }

            return string.Equals(mode, "upload", StringComparison.OrdinalIgnoreCase)
                ? "upload"
                : "manual";
        }

        private static string NormalizeQuizScope(string? scope)
        {
            return string.Equals(scope, "community", StringComparison.OrdinalIgnoreCase)
                ? "community"
                : "myquiz";
        }

        private static string GetCreateViewPath(string mode)
        {
            if (string.Equals(mode, "bank", StringComparison.OrdinalIgnoreCase))
            {
                return "~/Views/Event/EventQuiz/CreateQuiz/QuestionBank/Create.cshtml";
            }

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

            var normalized = correctAnswer.Replace(" ", string.Empty).ToUpperInvariant();
            if (!Regex.IsMatch(normalized, @"^[A-D](,[A-D])*$"))
            {
                return string.Empty;
            }

            var answers = normalized
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (typeOption == DataAccess.Enum.QuestionTypeOptionEnum.MultipleChoice)
            {
                return string.Join(",", answers);
            }

            return answers.FirstOrDefault() ?? string.Empty;
        }
    }
}
