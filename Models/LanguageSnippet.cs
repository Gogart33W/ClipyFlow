using System;

namespace ClipyFlow.Models
{
    public class LanguageSnippet
    {
        public string Language { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int UsageCount { get; set; } = 0;
    }
}
