using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Approver;
using BusinessLogic.DTOs.Event.Topic;
using BusinessLogic.Service.Event.Sub_Service.Topic;
using BusinessLogic.Service.ValidationData.Topic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Approver")]
    public class TopicController : BaseController
    {
        private readonly ITopicService _topicService;

        public TopicController(ITopicService topicService)
        {
            _topicService = topicService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var topics = await _topicService.GetAllTopicsAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                topics = topics
                    .Where(x => x.TopicName.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || x.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var vm = new TopicIndexViewModel
            {
                Search = search,
                Topics = topics.Select(x => new TopicListItemVm
                {
                    TopicId = x.TopicId,
                    TopicName = x.TopicName,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                }).ToList()
            };

            return View("~/Views/Topic/Index.cshtml", vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Topic/Create.cshtml", new CreateTopicViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTopicViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Topic/Create.cshtml", vm);
            }

            try
            {
                await _topicService.CreateTopicAsync(new CreateTopicDTO
                {
                    TopicName = vm.TopicName,
                    Description = vm.Description ?? string.Empty
                });
                SetSuccess("Tạo topic thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (TopicValidator.BusinessValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View("~/Views/Topic/Create.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var topic = await _topicService.GetTopicByIdAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            return View("~/Views/Topic/Edit.cshtml", new UpdateTopicViewModel
            {
                TopicId = topic.TopicId,
                TopicName = topic.TopicName,
                Description = topic.Description
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateTopicViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Topic/Edit.cshtml", vm);
            }

            try
            {
                var updated = await _topicService.UpdateTopicAsync(vm.TopicId, new UpdateTopicDTO
                {
                    TopicName = vm.TopicName,
                    Description = vm.Description ?? string.Empty
                });

                if (!updated)
                {
                    return NotFound();
                }

                SetSuccess("Cập nhật topic thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (TopicValidator.BusinessValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View("~/Views/Topic/Edit.cshtml", vm);
        }
    }
}
