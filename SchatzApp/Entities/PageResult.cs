namespace SchatzApp.Entities
{
    /// <summary>
    /// Returned by <see cref="SchatzApp.Logic.ApiController.GetPage"/> and used by <see cref="SchatzApp.Logic.IndexController"/>.
    /// </summary>
    public class PageResult
    {
        /// <summary>
        /// The requested page's title.
        /// </summary>
        public string Title;
        /// <summary>
        /// Keywords to include in header for page.
        /// </summary>
        public string Keywords;
        /// <summary>
        /// Description to include in header for page.
        /// </summary>
        public string Description;
        /// <summary>
        /// The HTML content (shown within single-page app's content element).
        /// </summary>
        public string Html;
    }
}
