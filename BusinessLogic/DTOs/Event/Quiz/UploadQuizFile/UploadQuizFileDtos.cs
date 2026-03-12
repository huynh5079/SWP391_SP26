namespace BusinessLogic.DTOs.Event.Quiz.UploadQuizFile
{
    public class UploadQuizFileRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = [];
        public string FileName { get; set; } = string.Empty;
    }

    public class UploadQuizFileResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string FileQuiz { get; set; } = string.Empty;
        public int ImportedQuestionCount { get; set; }
    }
}
