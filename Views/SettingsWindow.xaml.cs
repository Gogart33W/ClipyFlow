using System.Windows;
using ClipyFlow.Services;

namespace ClipyFlow.Views
{
    public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly StorageService _storageService;
        private readonly AppData _data;
        
        private readonly bool _initialStartWithWindows;
        private readonly string _initialTenorApiKey;
        private readonly bool _initialAutoPasteEnabled;
        private readonly string _initialTheme;

        public SettingsWindow(StorageService storageService, AppData data)
        {
            InitializeComponent();
            _storageService = storageService;
            _data = data;
            
            // Backup initial state
            _initialStartWithWindows = _data.Settings.StartWithWindows;
            _initialTenorApiKey = _data.Settings.TenorApiKey ?? "";
            _initialAutoPasteEnabled = _data.Settings.AutoPasteEnabled;
            _initialTheme = _data.Settings.Theme ?? "";
            
            // Set DataContext for binding
            DataContext = _data;
        }

        private void AutorunToggle_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Wpf.Ui.Controls.ToggleSwitch)
            {
                ToggleAutorun.IsChecked = !ToggleAutorun.IsChecked;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Restore original settings
            _data.Settings.StartWithWindows = _initialStartWithWindows;
            _data.Settings.TenorApiKey = _initialTenorApiKey;
            _data.Settings.AutoPasteEnabled = _initialAutoPasteEnabled;
            _data.Settings.Theme = _initialTheme;
            
            this.Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Apply Autorun changes
            var autorun = new AutorunService();
            autorun.SetAutorun(_data.Settings.StartWithWindows);

            // Save all settings
            _ = _storageService.SaveDataAsync(_data);
            
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                _data.Settings.Theme == "Light" ? Wpf.Ui.Appearance.ApplicationTheme.Light :
                _data.Settings.Theme == "Dark" ? Wpf.Ui.Appearance.ApplicationTheme.Dark :
                Wpf.Ui.Appearance.ApplicationTheme.Unknown
            );
            
            this.Close();
        }
    }
}
