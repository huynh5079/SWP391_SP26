using AEMS_Solution.Controllers.Common;
using BusinessLogic.DTOs.Event.Quiz.QuizForAll;
using BusinessLogic.Service.Event.Sub_Service.Quiz.ForAll;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Api
{
    [Authorize(Roles = "Student")]
    [Route("api/v1/student-quiz")]
    [ApiController]
    public class StudentQuizApiController : BaseController
    {
        private readonly IQuizServiceForAll _quizServiceForAll;
        private readonly IUnitOfWork _uow;

        public StudentQuizApiController(IQuizServiceForAll quizServiceForAll, IUnitOfWork uow)
        {
            _quizServiceForAll = quizServiceForAll;
            _uow = uow;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent([FromQuery] string quizId)
        {
            try
            {
                var studentId = await GetStudentProfileIdAsync();
                var result = await _quizServiceForAll.GetCurrentQuizSessionAsync(new GetCurrentQuizSessionRequestDto
                {
                    QuizId = quizId,
                    StudentId = studentId
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartQuizRequestDto request)
        {
            try
            {
                var studentId = await GetStudentProfileIdAsync();
                request.StudentId = studentId;
                var result = await _quizServiceForAll.StartQuizAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitQuizRequestDto request)
        {
            try
            {
                var studentId = await GetStudentProfileIdAsync();
                request.StudentId = studentId;
                var result = await _quizServiceForAll.SubmitQuizAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<string> GetStudentProfileIdAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                throw new InvalidOperationException("Không xác định được tài khoản hiện tại.");
            }

            var student = await _uow.StudentProfiles.GetAsync(x => x.UserId == CurrentUserId);
            if (student == null)
            {
                throw new InvalidOperationException("Không tìm thấy hồ sơ sinh viên.");
            }

            return student.Id;
        }
    }
}
