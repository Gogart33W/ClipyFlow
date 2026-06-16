using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClipyFlow.Models;
using ClipyFlow.Services;

namespace ClipyFlow.Views
{
    public partial class EmojiControl : UserControl
    {
        private readonly EmojiService _emojiService;
        public event EventHandler<EmojiItem>? EmojiSelected;

        public EmojiControl()
        {
            InitializeComponent();
            _emojiService = new EmojiService();
            LoadEmojis("");
        }

        private void LoadEmojis(string query)
        {
            var emojis = _emojiService.Search(query).ToList();
            EmojiList.ItemsSource = emojis;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadEmojis(SearchBox.Text);
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EmojiItem emoji)
            {
                EmojiSelected?.Invoke(this, emoji);
            }
        }
    }
}
