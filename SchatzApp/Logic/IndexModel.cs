using SchatzApp.Entities;

namespace SchatzApp.Logic
{
    /// <summary>
    /// All the page-specific info provided to Index.cshtml.
    /// </summary>
    public class IndexModel
    {
        /// <summary>
        /// Dynamic content to show.
        /// </summary>
        public readonly PageResult PR;
        /// <summary>
        /// Google Analytics code.
        /// </summary>
        public readonly string GACode;

        /// <summary>
        /// Ctor: init immutable instance.
        /// </summary>
        public IndexModel(PageResult pr, string gaCode)
        {
            PR = pr;
            GACode = gaCode;
        }
    }
}
