namespace SchatzApp.Entities
{
    /// <summary>
    /// Returned by <see cref="SchatzApp.Logic.ApiController.GetPage"/> and used by <see cref="SchatzApp.Logic.IndexController"/>.
    /// </summary>
    public class PageResult
    {
        /// <summary>
        /// If true, page must include "noindex" meta tag.
        /// </summary>
        public bool NoIndex { get; set; }
        /// <summary>
        /// The page's normalized relative URL.
        /// </summary>
        public string RelNorm { get; set; }
        /// <summary>
        /// The requested page's title.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Keywords to include in header for page.
        /// </summary>
        public string Keywords { get; set; }
        /// <summary>
        /// Description to include in header for page.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The HTML content (shown within single-page app's content element).
        /// </summary>
        public string Html { get; set; }
    }
}
