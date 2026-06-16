using System.Windows;
using ClipyFlow.Services;

namespace ClipyFlow.Views
{
    public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly StorageService _storageService;
        private readonly AppData _data;

        public SettingsWindow(StorageService storageService, AppData data)
        {
            InitializeComponent();
            _storageService = storageService;
            _data = data;
            
            // Set DataContext for binding
            DataContext = _data;
        }

        private void AutorunToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleAutorun.IsChecked = !ToggleAutorun.IsChecked;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Reload original settings from disk to cancel changes
            var freshData = _storageService.LoadData();
            _data.Settings.StartWithWindows = freshData.Settings.StartWithWindows;
            _data.Settings.TenorApiKey = freshData.Settings.TenorApiKey;
            _data.Settings.AutoPasteEnabled = freshData.Settings.AutoPasteEnabled;
            _data.Settings.Theme = freshData.Settings.Theme;
            
            this.Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Apply Autorun changes
            var autorun = new AutorunService();
            autorun.SetAutorun(_data.Settings.StartWithWindows);

            // Save all settings
            _storageService.SaveData(_data);
            
            this.Close();
        }
    }
}
