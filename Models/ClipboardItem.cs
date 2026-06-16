using System;
using System.Text.RegularExpressions;

namespace ClipyFlow.Models
{
    public enum ClipboardItemType
    {
        Text,
        Link,
        Code,
        Color,
        Image
    }

    public class ClipboardItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string Text { get; set; } = string.Empty;
        public DateTime CopiedAt { get; set; }
        public ClipboardItemType ItemType { get; set; } = ClipboardItemType.Text;
        public string? HexColor { get; set; }
        public string? ImagePath { get; set; }
        
        public string TimeGroup
        {
            get
            {
                var diff = DateTime.Now.Date - CopiedAt.Date;
                if (diff.Days == 0) return "Сьогодні";
                if (diff.Days == 1) return "Вчора";
                if (diff.Days <= 7) return "Останні 7 днів";
                return "Раніше";
            }
        }

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

        public static ClipboardItem? CreateImage(System.Windows.Media.Imaging.BitmapSource source)
        {
            try
            {
                string imagesDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "ClipyFlow", "images");
                
                System.IO.Directory.CreateDirectory(imagesDir);
                
                string fileName = $"img_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string fullPath = System.IO.Path.Combine(imagesDir, fileName);

                using (var fileStream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                {
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
                    encoder.Save(fileStream);
                }

                // Cleanup old images (keep last 20)
                var dir = new System.IO.DirectoryInfo(imagesDir);
                var files = dir.GetFiles("img_*.png").OrderByDescending(f => f.CreationTime).ToList();
                if (files.Count > 20)
                {
                    foreach (var file in files.Skip(20))
                    {
                        try { file.Delete(); } catch { }
                    }
                }

                return new ClipboardItem
                {
                    Text = "[Image]",
                    CopiedAt = DateTime.Now,
                    ItemType = ClipboardItemType.Image,
                    ImagePath = fullPath
                };
            }
            catch
            {
                return null;
            }
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

            // Check if Code (better heuristics)
            bool isCode = false;
            if (Regex.IsMatch(trimmed, @"\b(public|private|protected|class|function|var|let|const|def|import|using|namespace)\b\s+[\w\$\_]+")) isCode = true;
            else if (Regex.IsMatch(trimmed, @"<[a-z0-9]+[^>]*>.*<\/[a-z0-9]+>", RegexOptions.IgnoreCase | RegexOptions.Singleline)) isCode = true; // HTML/XML
            else if (Regex.IsMatch(trimmed, @"\w+\s*\(.*\)\s*\{")) isCode = true; // function signature
            else if (Regex.IsMatch(trimmed, @"^\{\s*""\w+""\s*:")) isCode = true; // JSON like

            if (isCode)
            {
                ItemType = ClipboardItemType.Code;
                return;
            }

            // Default
            ItemType = ClipboardItemType.Text;
        }
    }
}
