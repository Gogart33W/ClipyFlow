using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClipyFlow.Services;

namespace ClipyFlow.Views
{
    public partial class GifControl : UserControl
    {
        private readonly TenorService _tenorService;
        private string _apiKey = string.Empty;
        
        public event EventHandler<string>? GifSelected;

        public GifControl()
        {
            InitializeComponent();
            _tenorService = new TenorService();
        }

        public void Initialize(string apiKey)
        {
            _apiKey = apiKey;
            if (string.IsNullOrEmpty(_apiKey))
            {
                TxtStatus.Text = "Please set Tenor API Key in Settings.";
                TxtStatus.Visibility = Visibility.Visible;
                return;
            }
            
            LoadFeatured();
        }

        private async void LoadFeatured()
        {
            TxtStatus.Text = "Loading featured GIFs...";
            TxtStatus.Visibility = Visibility.Visible;
            GifList.ItemsSource = null;
            
            var gifs = await _tenorService.GetFeaturedGifsAsync(_apiKey);
            
            GifList.ItemsSource = gifs;
            TxtStatus.Visibility = Visibility.Collapsed;
        }

        private async void PerformSearch()
        {
            if (string.IsNullOrEmpty(_apiKey)) return;
            
            string query = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                LoadFeatured();
                return;
            }
            
            TxtStatus.Text = "Searching...";
            TxtStatus.Visibility = Visibility.Visible;
            GifList.ItemsSource = null;
            
            var gifs = await _tenorService.SearchGifsAsync(_apiKey, query);
            
            GifList.ItemsSource = gifs;
            TxtStatus.Visibility = Visibility.Collapsed;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void Gif_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is GifItem gif)
            {
                GifSelected?.Invoke(this, gif.Url);
            }
        }
    }
}
