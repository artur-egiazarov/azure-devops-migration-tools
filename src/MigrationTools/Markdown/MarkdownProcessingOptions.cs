using MigrationTools.Tools.Infrastructure;

namespace MigrationTools.Markdown
{
    /// <summary>
    /// Configuration options for Markdown processing during work item field migration.
    /// Enables automatic detection and conversion of Markdown content in fields to prevent data corruption.
    /// </summary>
    public class MarkdownProcessingOptions : ToolOptions
    {
        /// <summary>
        /// Automatically detect Markdown in fields and convert to HTML.
        /// When true, fields containing Markdown syntax will be converted to HTML format.
        /// <default>true</default>
        /// </summary>
        public bool AutoDetectAndConvertMarkdown { get; set; } = true;

        /// <summary>
        /// List of field reference names that should have Markdown conversion applied.
        /// If empty, all string fields will be checked for Markdown.
        /// Example: ["System.Description", "System.History", "Microsoft.VSTS.Common.Symptom"]
        /// <default></default>
        /// </summary>
        public string[] FieldsToProcess { get; set; } = new string[] { };

        /// <summary>
        /// List of field reference names that should NOT have Markdown conversion applied.
        /// Use this to exclude specific fields from Markdown processing.
        /// <default></default>
        /// </summary>
        public string[] FieldsToExclude { get; set; } = new string[] { };

        /// <summary>
        /// Target format for Markdown conversion. Options: "Html" or "PlainText"
        /// "Html" converts Markdown to HTML format (default for rich text fields).
        /// "PlainText" removes all Markdown formatting but preserves content.
        /// <default>Html</default>
        /// </summary>
        public MarkdownTargetFormat TargetFormat { get; set; } = MarkdownTargetFormat.Html;

        /// <summary>
        /// Log detected Markdown fields for troubleshooting.
        /// <default>false</default>
        /// </summary>
        public bool LogDetectedMarkdown { get; set; } = false;
    }

    /// <summary>
    /// Specifies the target format for Markdown conversion.
    /// </summary>
    public enum MarkdownTargetFormat
    {
        /// <summary>
        /// Convert Markdown to HTML format, preserving all formatting.
        /// </summary>
        Html = 0,

        /// <summary>
        /// Convert Markdown to plain text, removing all formatting.
        /// </summary>
        PlainText = 1
    }
}
