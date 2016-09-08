namespace SchatzApp.Entities
{
    /// <summary>
    /// Returned by <see cref="SchatzApp.Logic.ApiController.GetQuiz"/>.
    /// </summary>
    public class QuizResult
    {
        public string[] Words1;
        public string[] Words2;
    }
}
