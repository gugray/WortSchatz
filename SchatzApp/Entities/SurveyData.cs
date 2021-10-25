namespace SchatzApp.Entities
{
    /// <summary>
    /// <para>Represents survey data submitted by JS app along with user's quiz choices.</para>
    /// <para>See quiz.js.</para>
    /// </summary>
    public class SurveyData
    {
        public string Native { get; set; }
        public string Age { get; set; }
        public string NativeCountry { get; set; }
        public string NativeEducation { get; set; }
        public string NativeOtherLangs { get; set; }
        public string NnCountryNow { get; set; }
        public string NnGermanTime { get; set; }
        public string NnGermanLevel { get; set; }
    }
}
