using AEMS_Solution.Controllers.Common;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AEMS_Solution.Models.Admin;
using System.Linq;
using System.Threading.Tasks;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Admin")]
    public class AdminFeedbackController : BaseController
    {
        private readonly IUnitOfWork _uow;

        public AdminFeedbackController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IActionResult> Index()
        {
            var feedbacks = await _uow.Feedbacks.GetAllAsync(null,
                query => query.Include(f => f.Event).Include(f => f.Student).ThenInclude(s => s.User));

            var viewModel = feedbacks.Select(f => new AdminFeedbackViewModel
            {
                Id = f.Id,
                EventName = f.Event?.Title ?? "Unknown Event",
                StudentName = f.Student?.User != null ? f.Student.User.FullName : "Unknown Student",
                StudentCode = f.Student?.StudentCode ?? "",
                Comment = f.Comment,
                Status = f.Status,
                Rating = f.RatingEvent,
                CreatedAt = f.CreatedAt
            }).OrderByDescending(f => f.CreatedAt).ToList();

            return View(viewModel);
        }
    }
}
