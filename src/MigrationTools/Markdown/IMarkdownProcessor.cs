namespace MigrationTools.Markdown
{
    /// <summary>
    /// Interface for processing Markdown content in work item fields.
    /// </summary>
    public interface IMarkdownProcessor
    {
        /// <summary>
        /// Detects if the provided text contains Markdown syntax.
        /// </summary>
        /// <param name="text">The text to check for Markdown syntax.</param>
        /// <returns>True if Markdown syntax is detected, false otherwise.</returns>
        bool ContainsMarkdown(string text);

        /// <summary>
        /// Converts Markdown text to HTML format.
        /// </summary>
        /// <param name="markdown">The Markdown text to convert.</param>
        /// <returns>The HTML representation of the Markdown text.</returns>
        string ConvertMarkdownToHtml(string markdown);

        /// <summary>
        /// Converts Markdown text to plain text by removing all formatting.
        /// </summary>
        /// <param name="markdown">The Markdown text to convert.</param>
        /// <returns>The plain text without Markdown formatting.</returns>
        string ConvertMarkdownToPlainText(string markdown);

        /// <summary>
        /// Detects if text is in HTML format by checking for HTML tags.
        /// </summary>
        public bool IsHtmlFormat(string text);

        /// <summary>
        /// Detects if text is in Markdown format (not HTML).
        /// </summary>
        public bool IsMarkdownFormat(string text);
    }
}
