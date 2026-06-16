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
            _allEmojis = new List<EmojiItem>
            {
                new EmojiItem { Character = "😀", Name = "grinning face", Category = "Smileys" },
                new EmojiItem { Character = "😂", Name = "face with tears of joy", Category = "Smileys" },
                new EmojiItem { Character = "😍", Name = "smiling face with heart-eyes", Category = "Smileys" },
                new EmojiItem { Character = "🥰", Name = "smiling face with hearts", Category = "Smileys" },
                new EmojiItem { Character = "😎", Name = "smiling face with sunglasses", Category = "Smileys" },
                new EmojiItem { Character = "🤔", Name = "thinking face", Category = "Smileys" },
                new EmojiItem { Character = "👍", Name = "thumbs up", Category = "People" },
                new EmojiItem { Character = "👎", Name = "thumbs down", Category = "People" },
                new EmojiItem { Character = "🙌", Name = "raising hands", Category = "People" },
                new EmojiItem { Character = "👏", Name = "clapping hands", Category = "People" },
                new EmojiItem { Character = "❤️", Name = "red heart", Category = "Symbols" },
                new EmojiItem { Character = "🔥", Name = "fire", Category = "Objects" },
                new EmojiItem { Character = "✨", Name = "sparkles", Category = "Symbols" },
                new EmojiItem { Character = "⭐", Name = "star", Category = "Symbols" },
                new EmojiItem { Character = "✅", Name = "check mark button", Category = "Symbols" },
                new EmojiItem { Character = "❌", Name = "cross mark", Category = "Symbols" },
                new EmojiItem { Character = "🚀", Name = "rocket", Category = "Travel" },
                new EmojiItem { Character = "🎉", Name = "party popper", Category = "Objects" },
                new EmojiItem { Character = "💻", Name = "laptop", Category = "Objects" },
                new EmojiItem { Character = "📱", Name = "mobile phone", Category = "Objects" },
                new EmojiItem { Character = "🐶", Name = "dog face", Category = "Animals" },
                new EmojiItem { Character = "🐱", Name = "cat face", Category = "Animals" },
                new EmojiItem { Character = "🍎", Name = "red apple", Category = "Food" },
                new EmojiItem { Character = "🍕", Name = "pizza", Category = "Food" }
            };
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
