using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs.Student
{
    public class SubmitFeedbackRequestDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
