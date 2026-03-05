using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MigrationTools.Markdown
{
    /// <summary>
    /// Processes Markdown content by detecting and converting it to HTML format.
    /// This tool helps preserve formatting when migrating fields that contain Markdown syntax.
    /// </summary>
    public class MarkdownProcessor : IMarkdownProcessor
    {
        // Compiled once; shared across all instances and all calls.
        private static readonly Lazy<List<Regex>> MarkdownPatterns = new Lazy<List<Regex>>(() =>
        {
            return new List<Regex>
            {
                new Regex(@"^#+\s+", RegexOptions.Multiline),                           // # Headings
                new Regex(@"\*\*.*?\*\*|__.*?__"),                                      // **bold** or __bold__
                new Regex(@"\*(?!\s).*?\*|_(?!\s).*?_"),                               // *italic* or _italic_
                new Regex(@"`.*?`"),                                                    // `code`
                new Regex(@"\[.*?\]\(.*?\)"),                                           // [link](url)
                new Regex(@"^[-*]\s+", RegexOptions.Multiline),                        // - or * list items
                new Regex(@"^>\s+", RegexOptions.Multiline),                           // > blockquotes
                new Regex(@"~~.*?~~"),                                                  // ~~strikethrough~~
                new Regex(@"!\[.*?\]\(.*?\)"),                                         // ![alt](image)
                new Regex(@"^\d+\.\s+", RegexOptions.Multiline),                       // 1. numbered list
            };
        });

        // Pre-compiled; used frequently during encoding to detect already-encoded entities.
        private static readonly Regex HtmlEntityPattern =
            new Regex(@"^&(amp|lt|gt|quot|apos|#\d+|#x[0-9a-fA-F]+);$", RegexOptions.Compiled);

        /// <summary>
        /// Detects if the provided text contains Markdown syntax.
        /// </summary>
        /// <param name="text">The text to check for Markdown syntax.</param>
        /// <returns>True if Markdown syntax is detected, false otherwise.</returns>
        public bool ContainsMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (var pattern in MarkdownPatterns.Value)
            {
                if (pattern.IsMatch(text))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Detects if text is in HTML format by checking for HTML tags.
        /// HTML fields start with opening tag like &lt; and contain proper HTML structure.
        /// </summary>
        public bool IsHtmlFormat(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var trimmed = text.Trim();
            return Regex.IsMatch(trimmed, @"^\s*<[a-z/!]", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Detects if text is in Markdown format.
        /// Markdown should NOT start with HTML tag and must contain Markdown patterns.
        /// </summary>
        public bool IsMarkdownFormat(string text)
        {
            if (string.IsNullOrEmpty(text) || IsHtmlFormat(text))
            {
                return false;
            }

            return ContainsMarkdown(text);
        }

        /// <summary>
        /// Converts Markdown text to HTML format.
        /// </summary>
        /// <param name="markdown">The Markdown text to convert.</param>
        /// <returns>The HTML representation of the Markdown text.</returns>
        public string ConvertMarkdownToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }

            var html = new StringBuilder();
            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var inCodeBlock = false;
            var inList = false;
            var inBlockquote = false;
            var listType = ""; // "ul" or "ol"

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.TrimStart();

                // Handle code blocks
                if (trimmedLine.StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        html.Append("</code></pre>\n");
                        inCodeBlock = false;
                    }
                    else
                    {
                        var lang = trimmedLine.Substring(3).Trim();
                        html.Append($"<pre><code class=\"language-{lang}\">\n");
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    html.Append(HtmlEncode(line)).Append("\n");
                    continue;
                }

                // Handle headings
                if (trimmedLine.StartsWith("#"))
                {
                    var level = 0;
                    for (int j = 0; j < trimmedLine.Length && trimmedLine[j] == '#'; j++)
                    {
                        level++;
                    }

                    if (level <= 6)
                    {
                        var content = trimmedLine.Substring(level).Trim();
                        content = ConvertInlineMarkdown(content);
                        html.Append($"<h{level}>{content}</h{level}>\n");
                        continue;
                    }
                }

                // Handle blockquotes
                if (trimmedLine.StartsWith(">"))
                {
                    if (!inBlockquote)
                    {
                        html.Append("<blockquote>\n");
                        inBlockquote = true;
                    }

                    var content = trimmedLine.Substring(1).TrimStart();
                    content = ConvertInlineMarkdown(content);
                    html.Append($"<p>{content}</p>\n");
                    continue;
                }

                if (inBlockquote && !trimmedLine.StartsWith(">"))
                {
                    html.Append("</blockquote>\n");
                    inBlockquote = false;
                }

                // Handle unordered lists
                if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ") || trimmedLine.StartsWith("+ "))
                {
                    if (!inList || listType != "ul")
                    {
                        if (inList)
                        {
                            html.Append("</ol>\n");
                        }

                        html.Append("<ul>\n");
                        inList = true;
                        listType = "ul";
                    }

                    var content = trimmedLine.Substring(2);
                    content = ConvertInlineMarkdown(content);
                    html.Append($"<li>{content}</li>\n");
                    continue;
                }

                // Handle ordered lists
                var orderedListMatch = Regex.Match(trimmedLine, @"^\d+\.\s+");
                if (orderedListMatch.Success)
                {
                    if (!inList || listType != "ol")
                    {
                        if (inList)
                        {
                            html.Append("</ul>\n");
                        }

                        html.Append("<ol>\n");
                        inList = true;
                        listType = "ol";
                    }

                    var content = trimmedLine.Substring(orderedListMatch.Length);
                    content = ConvertInlineMarkdown(content);
                    html.Append($"<li>{content}</li>\n");
                    continue;
                }

                // Close lists if we hit a non-list line
                if (inList && !trimmedLine.StartsWith("- ") && !trimmedLine.StartsWith("* ") && 
                    !trimmedLine.StartsWith("+ ") && !orderedListMatch.Success && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    html.Append($"</{listType}>\n");
                    inList = false;
                }

                // Handle paragraphs
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    var content = ConvertInlineMarkdown(trimmedLine);
                    html.Append($"<p>{content}</p>\n");
                }
                else
                {
                    // Empty line
                    html.Append("\n");
                }
            }

            // Close any open tags
            if (inList)
            {
                html.Append($"</{listType}>\n");
            }

            if (inBlockquote)
            {
                html.Append("</blockquote>\n");
            }

            if (inCodeBlock)
            {
                html.Append("</code></pre>\n");
            }

            return html.ToString().TrimEnd();
        }

        /// <summary>
        /// Converts Markdown text to plain text by removing all formatting.
        /// </summary>
        /// <param name="markdown">The Markdown text to convert.</param>
        /// <returns>The plain text without Markdown formatting.</returns>
        public string ConvertMarkdownToPlainText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }

            var text = markdown;

            // Remove headings
            text = Regex.Replace(text, @"^#+\s+", "", RegexOptions.Multiline);

            // Remove bold
            text = Regex.Replace(text, @"\*\*(.*?)\*\*", "$1");
            text = Regex.Replace(text, @"__(.*?)__", "$1");

            // Remove italic
            text = Regex.Replace(text, @"\*(.*?)\*", "$1");
            text = Regex.Replace(text, @"_(.*?)_", "$1");

            // Remove code highlighting
            text = Regex.Replace(text, @"`(.*?)`", "$1");

            // Remove links but keep text
            text = Regex.Replace(text, @"\[(.*?)\]\(.*?\)", "$1");

            // Remove images
            text = Regex.Replace(text, @"!\[.*?\]\(.*?\)", "");

            // Remove strikethrough
            text = Regex.Replace(text, @"~~(.*?)~~", "$1");

            // Remove blockquote markers
            text = Regex.Replace(text, @"^>\s+", "", RegexOptions.Multiline);

            // Remove list markers
            text = Regex.Replace(text, @"^[-*+]\s+", "", RegexOptions.Multiline);
            text = Regex.Replace(text, @"^\d+\.\s+", "", RegexOptions.Multiline);

            // Remove code block markers
            text = Regex.Replace(text, @"```.*?\n", "");
            text = Regex.Replace(text, @"```", "");

            // Clean up extra whitespace
            text = Regex.Replace(text, @"\n\s*\n", "\n");

            return text.Trim();
        }

        /// <summary>
        /// Converts inline Markdown elements (bold, italic, links, images, etc.) to HTML.
        /// Processing order matters:
        ///   1. Images before links — ![alt](url) would otherwise be partially captured by the link regex.
        ///   2. Images/links before HtmlEncode — preserves raw '&amp;' in query strings so that
        ///      TfsEmbededImagesTool can download from the unmodified URL.
        ///   3. Bold/italic/code only on text segments outside already-inserted HTML tags —
        ///      prevents accidental matches on underscores or asterisks inside attribute values.
        /// </summary>
        private static string ConvertInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 1. Images (must precede the link regex and encoding)
            text = Regex.Replace(text, @"!\[([^\]]*)\]\(([^)]+)\)", m =>
                $"<img src=\"{m.Groups[2].Value}\" alt=\"{HtmlEncodeText(m.Groups[1].Value)}\" />");

            // 2. Links (must precede encoding)
            text = Regex.Replace(text, @"\[(.+?)\]\((.+?)\)", m =>
                $"<a href=\"{m.Groups[2].Value}\">{HtmlEncodeText(m.Groups[1].Value)}</a>");

            // 3. Encode remaining plain text (outside already-generated HTML tags)
            text = HtmlEncodeOutsideTags(text);

            // 4. Inline formatting — applied only to text nodes; underscores/asterisks inside
            //    attribute values of <img>/<a> tags are skipped by TransformTextSegments.
            text = TransformTextSegments(text, t =>
            {
                t = Regex.Replace(t, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
                t = Regex.Replace(t, @"__(.+?)__",     "<strong>$1</strong>");
                t = Regex.Replace(t, @"\*(.+?)\*",     "<em>$1</em>");
                t = Regex.Replace(t, @"_(.+?)_",       "<em>$1</em>");
                t = Regex.Replace(t, @"`(.+?)`",        "<code>$1</code>");
                t = Regex.Replace(t, @"~~(.+?)~~",      "<del>$1</del>");
                return t;
            });

            return text;
        }

        /// <summary>
        /// Applies <paramref name="transform"/> only to the plain-text segments of
        /// <paramref name="text"/>, leaving HTML tags (and their attribute values) untouched.
        /// </summary>
        private static string TransformTextSegments(string text, Func<string, string> transform)
        {
            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<')
                {
                    int end = text.IndexOf('>', i);
                    if (end < 0)
                    {
                        // Unclosed tag — treat remainder as plain text
                        sb.Append(transform(text.Substring(i)));
                        break;
                    }
                    sb.Append(text, i, end - i + 1);  // copy tag verbatim
                    i = end + 1;
                }
                else
                {
                    int next = text.IndexOf('<', i);
                    if (next < 0) next = text.Length;
                    sb.Append(transform(text.Substring(i, next - i)));
                    i = next;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// HTML-encodes special characters in the plain-text segments of <paramref name="text"/>,
        /// leaving already-generated HTML tags (including their attribute values) untouched.
        /// </summary>
        private static string HtmlEncodeOutsideTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<')
                {
                    int end = text.IndexOf('>', i);
                    if (end < 0)
                    {
                        sb.Append("&lt;");  // malformed — encode the stray '<'
                        i++;
                    }
                    else
                    {
                        sb.Append(text, i, end - i + 1);  // copy tag verbatim
                        i = end + 1;
                    }
                }
                else if (text[i] == '&')
                {
                    // Preserve already-encoded HTML entities (&amp; &lt; &#42; &#x2F; …)
                    int entityEnd = text.IndexOf(';', i);
                    if (entityEnd > i && entityEnd - i <= 10)
                    {
                        var entity = text.Substring(i, entityEnd - i + 1);
                        if (HtmlEntityPattern.IsMatch(entity))
                        {
                            sb.Append(entity);
                            i = entityEnd + 1;
                            continue;
                        }
                    }
                    sb.Append("&amp;");
                    i++;
                }
                else if (text[i] == '>')
                {
                    sb.Append("&gt;");
                    i++;
                }
                else
                {
                    sb.Append(text[i++]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Encodes HTML special characters in a known-plain string (alt text, link text).
        /// No entity-preservation needed here because the input is raw user text.
        /// </summary>
        private static string HtmlEncodeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        /// <summary>
        /// HTML-encodes a string, skipping already-encoded entities.
        /// Used for code block content where Markdown is not interpreted.
        /// </summary>
        private static string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '&')
                {
                    int entityEnd = text.IndexOf(';', i);
                    if (entityEnd > i && entityEnd - i <= 10)
                    {
                        var entity = text.Substring(i, entityEnd - i + 1);
                        if (HtmlEntityPattern.IsMatch(entity))
                        {
                            sb.Append(entity);
                            i = entityEnd + 1;
                            continue;
                        }
                    }
                    sb.Append("&amp;");
                    i++;
                }
                else
                {
                    switch (text[i])
                    {
                        case '<':  sb.Append("&lt;");   break;
                        case '>':  sb.Append("&gt;");   break;
                        case '"':  sb.Append("&quot;"); break;
                        case '\'': sb.Append("&#39;");  break;
                        default:   sb.Append(text[i]);  break;
                    }
                    i++;
                }
            }
            return sb.ToString();
        }
    }
}
