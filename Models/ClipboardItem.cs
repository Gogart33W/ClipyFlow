using System;
using System.Text.RegularExpressions;

namespace ClipyFlow.Models
{
    public enum ClipboardItemType
    {
        Text,
        Link,
        Code,
        Color
    }

    public class ClipboardItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string Text { get; set; } = string.Empty;
        public DateTime CopiedAt { get; set; }
        public ClipboardItemType ItemType { get; set; } = ClipboardItemType.Text;
        public string? HexColor { get; set; }

        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsPinned)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public static ClipboardItem Create(string text)
        {
            // Truncate to avoid memory issues with huge text
            const int MaxLength = 100000;
            if (text.Length > MaxLength)
            {
                text = text.Substring(0, MaxLength) + "\n\n...[TRUNCATED: Text too long]";
            }

            var item = new ClipboardItem
            {
                Text = text,
                CopiedAt = DateTime.Now
            };

            item.ParseType();
            return item;
        }

        private void ParseType()
        {
            if (string.IsNullOrWhiteSpace(Text)) return;

            string trimmed = Text.Trim();

            // Check if Color (HEX)
            var hexMatch = Regex.Match(trimmed, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
            if (hexMatch.Success)
            {
                ItemType = ClipboardItemType.Color;
                HexColor = trimmed;
                return;
            }

            // Check if Link
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ItemType = ClipboardItemType.Link;
                return;
            }

            // Check if Code (very basic heuristic)
            if (trimmed.Contains("public ") || trimmed.Contains("private ") || 
                trimmed.Contains("function ") || trimmed.Contains("=>") || 
                trimmed.Contains("const ") || trimmed.Contains("let ") ||
                (trimmed.Contains("{") && trimmed.Contains("}")) ||
                trimmed.StartsWith("<") && trimmed.EndsWith(">"))
            {
                ItemType = ClipboardItemType.Code;
                return;
            }

            // Default
            ItemType = ClipboardItemType.Text;
        }
    }
}
