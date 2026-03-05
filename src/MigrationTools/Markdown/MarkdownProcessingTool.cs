using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MigrationTools.Tools.Infrastructure;
using MigrationTools.Tools.Interfaces;

namespace MigrationTools.Markdown
{
    /// <summary>
    /// Tool for detecting and converting Markdown content in work item fields during migration.
    /// Prevents data corruption when fields contain Markdown syntax by automatically converting
    /// to HTML or plain text format based on configuration.
    /// </summary>
    public class MarkdownProcessingTool : Tool<MarkdownProcessingOptions>, IMarkdownProcessor
    {
        private readonly IMarkdownProcessor _processor;

        public MarkdownProcessingTool(
            IOptions<MarkdownProcessingOptions> options,
            IServiceProvider services,
            ILogger<MarkdownProcessingTool> logger,
            ITelemetryLogger telemetryLogger)
            : base(options, services, logger, telemetryLogger)
        {
            _processor = new MarkdownProcessor();
        }

        /// <summary>
        /// Processes a field value by detecting and converting Markdown if necessary.
        /// </summary>
        /// <param name="fieldValue">The field value to process.</param>
        /// <param name="fieldReferenceName">The reference name of the field being processed.</param>
        /// <returns>The processed field value with Markdown converted if detected.</returns>
        public string ProcessFieldValue(string fieldValue, string fieldReferenceName)
        {
            if (!Options.Enabled || string.IsNullOrEmpty(fieldValue))
            {
                return fieldValue;
            }

            if (!ShouldProcessField(fieldReferenceName))
            {
                return fieldValue;
            }

            // Skip if already in HTML format
            if (_processor.IsHtmlFormat(fieldValue))
            {
                return fieldValue;
            }

            // Only process if actual Markdown detected
            if (!_processor.IsMarkdownFormat(fieldValue))
            {
                return fieldValue;
            }

            if (Options.LogDetectedMarkdown)
            {
                Log.LogInformation("Detected Markdown in field {FieldName}. Converting to {TargetFormat} format.",
                    fieldReferenceName, Options.TargetFormat);
            }

            // Convert Markdown based on target format
            string result = Options.TargetFormat switch
            {
                MarkdownTargetFormat.Html => _processor.ConvertMarkdownToHtml(fieldValue),
                MarkdownTargetFormat.PlainText => _processor.ConvertMarkdownToPlainText(fieldValue),
                _ => fieldValue
            };

            return result;
        }

        /// <summary>
        /// Determines whether the provided text is already in HTML format.
        /// </summary>
        public bool IsHtmlFormat(string text)
        {
            if (!Options.Enabled)
            {
                return false;
            }

            return _processor.IsHtmlFormat(text);
        }

        /// <summary>
        /// Determines whether the provided text contains Markdown format.
        /// </summary>
        public bool IsMarkdownFormat(string text)
        {
            if (!Options.Enabled)
            {
                return false;
            }

            return _processor.IsMarkdownFormat(text);
        }

        /// <summary>
        /// Detects if the provided text contains Markdown syntax.
        /// </summary>
        public bool ContainsMarkdown(string text)
        {            
            if (!Options.Enabled)
            {
                return false;
            }

            return _processor.ContainsMarkdown(text);
        }

        /// <summary>
        /// Converts Markdown text to HTML format.
        /// </summary>
        public string ConvertMarkdownToHtml(string markdown)
        {
            if (!Options.Enabled)
            {
                return markdown;
            }

            return _processor.ConvertMarkdownToHtml(markdown);
        }

        /// <summary>
        /// Converts Markdown text to plain text by removing all formatting.
        /// </summary>
        public string ConvertMarkdownToPlainText(string markdown)
        {
            if (!Options.Enabled)
            {
                return markdown;
            }

            return _processor.ConvertMarkdownToPlainText(markdown);
        }

        /// <summary>
        /// Determines if a field should be processed based on include/exclude lists.
        /// </summary>
        private bool ShouldProcessField(string fieldReferenceName)
        {
            if (!Options.AutoDetectAndConvertMarkdown)
            {
                return false;
            }

            // If exclude list contains the field, don't process
            if (Options.FieldsToExclude?.Any(f => f.Equals(fieldReferenceName, StringComparison.OrdinalIgnoreCase)) == true)
            {
                return false;
            }

            // If include list is empty, process all fields
            if (Options.FieldsToProcess?.Length == 0)
            {
                return true;
            }

            // If include list is specified, only process listed fields
            return Options.FieldsToProcess?.Any(f => f.Equals(fieldReferenceName, StringComparison.OrdinalIgnoreCase)) == true;
        }
    }
}
