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
        
        private readonly string _initialCustomBackgroundPath;
        private readonly string _initialCustomBackgroundColor;
        private readonly double _initialCustomBackgroundOpacity;

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
            _initialCustomBackgroundPath = _data.Settings.CustomBackgroundPath ?? "";
            _initialCustomBackgroundColor = _data.Settings.CustomBackgroundColor ?? "";
            _initialCustomBackgroundOpacity = _data.Settings.CustomBackgroundOpacity;
            
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
            _data.Settings.CustomBackgroundPath = _initialCustomBackgroundPath;
            _data.Settings.CustomBackgroundColor = _initialCustomBackgroundColor;
            _data.Settings.CustomBackgroundOpacity = _initialCustomBackgroundOpacity;
            
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
            
            if (Application.Current.MainWindow is MainWindow main)
            {
                main.UpdateCustomBackground();
            }
            
            this.Close();
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All Files (*.*)|*.*",
                Title = "Select Background Image"
            };

            if (dlg.ShowDialog() == true)
            {
                _data.Settings.CustomBackgroundPath = dlg.FileName;
                // Force UI update since it's not observable
                TextBgPath.Text = dlg.FileName;
            }
        }

        private void BtnClearColor_Click(object sender, RoutedEventArgs e)
        {
            _data.Settings.CustomBackgroundColor = string.Empty;
            TextBgColor.Text = string.Empty;
        }
    }
}
