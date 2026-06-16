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

        private System.Collections.ObjectModel.ObservableCollection<EmojiItem> _emojiCollection = new();
        private System.Threading.CancellationTokenSource? _loadCts;

        private async void LoadEmojis(string query)
        {
            _loadCts?.Cancel();
            _loadCts = new System.Threading.CancellationTokenSource();
            var token = _loadCts.Token;

            var emojis = _emojiService.Search(query).ToList();
            
            EmojiList.ItemsSource = null;
            _emojiCollection.Clear();
            EmojiList.ItemsSource = _emojiCollection;

            if (emojis.Count == 0) return;

            // Load first chunk instantly
            var firstChunk = emojis.Take(50).ToList();
            foreach (var item in firstChunk) _emojiCollection.Add(item);

            // Load the rest asynchronously to keep UI responsive
            if (emojis.Count > 50)
            {
                var remaining = emojis.Skip(50).ToList();
                for (int i = 0; i < remaining.Count; i += 50)
                {
                    if (token.IsCancellationRequested) break;
                    var chunk = remaining.Skip(i).Take(50).ToList();
                    
                    await System.Threading.Tasks.Task.Delay(10, token).ConfigureAwait(true);
                    
                    if (token.IsCancellationRequested) break;
                    foreach (var item in chunk) _emojiCollection.Add(item);
                }
            }
        }

        private System.Windows.Threading.DispatcherTimer? _searchTimer;

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_searchTimer == null)
            {
                _searchTimer = new System.Windows.Threading.DispatcherTimer();
                _searchTimer.Interval = TimeSpan.FromMilliseconds(200);
                _searchTimer.Tick += (s, ev) => 
                {
                    _searchTimer.Stop();
                    LoadEmojis(SearchBox.Text);
                };
            }
            _searchTimer.Stop();
            _searchTimer.Start();
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
