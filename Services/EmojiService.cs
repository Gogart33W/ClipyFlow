using System.Collections.Generic;
using System.Linq;
using ClipyFlow.Models;

namespace ClipyFlow.Services
{
    public class EmojiService
    {
        private readonly List<EmojiItem> _allEmojis;

        public EmojiService()
        {
            _allEmojis = new List<EmojiItem>();
            LoadEmojis();
        }

        private void LoadEmojis()
        {
            try
            {
                string path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "emoji.json");
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        try
                        {
                            if (element.TryGetProperty("emoji", out var emojiProp) && 
                                element.TryGetProperty("description", out var descProp) && 
                                element.TryGetProperty("category", out var catProp))
                            {
                                string? emojiStr = emojiProp.ValueKind == System.Text.Json.JsonValueKind.String ? emojiProp.GetString() : null;
                                if (!string.IsNullOrEmpty(emojiStr))
                                {
                                    _allEmojis.Add(new EmojiItem
                                    {
                                        Character = emojiStr,
                                        Name = descProp.ValueKind == System.Text.Json.JsonValueKind.String ? descProp.GetString() ?? "" : "",
                                        Category = catProp.ValueKind == System.Text.Json.JsonValueKind.String ? catProp.GetString() ?? "" : ""
                                    });
                                }
                            }
                        }
                        catch { /* skip invalid emoji entry */ }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load emoji.json: {ex}");
            }
            
            if (_allEmojis.Count == 0)
            {
                // Fallback basic list
                _allEmojis.Add(new EmojiItem { Character = "😀", Name = "grinning face", Category = "Smileys" });
                _allEmojis.Add(new EmojiItem { Character = "❤️", Name = "red heart", Category = "Symbols" });
                _allEmojis.Add(new EmojiItem { Character = "👍", Name = "thumbs up", Category = "People" });
            }
        }

        public IEnumerable<EmojiItem> GetAll() => _allEmojis;

        public IEnumerable<EmojiItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _allEmojis;

            query = query.ToLowerInvariant();
            return _allEmojis.Where(e => e.Name.ToLowerInvariant().Contains(query));
        }
    }
}
