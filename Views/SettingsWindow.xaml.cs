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
            DataContext = this;
            
            ModernColors = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "#FF3B30", "#FF9500", "#FFCC00", "#34C759", "#00C7BE", "#007AFF", "#5856D6", "#AF52DE", "#FF2D55", "#8E8E93",
                "#FF453A", "#FF9F0A", "#FFD60A", "#32D74B", "#64D2FF", "#0A84FF", "#5E5CE6", "#BF5AF2", "#FF375F", "#98989D",
                "#D0021B", "#F5A623", "#F8E71C", "#8B572A", "#7ED321", "#417505", "#BD10E0", "#9013FE", "#4A90E2", "#50E3C2",
                "#B8E986", "#000000", "#4A4A4A", "#9B9B9B", "#FFFFFF"
            };
        }

        public AppData Data => _data;
        public System.Collections.ObjectModel.ObservableCollection<string> ModernColors { get; }

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
            
            App.UpdateGlobalHotkey(_data.Settings.GlobalHotkey);
            
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

        private void BtnClearImage_Click(object sender, RoutedEventArgs e)
        {
            _data.Settings.CustomBackgroundPath = string.Empty;
            TextBgPath.Text = string.Empty;
        }

        private void BtnClearColor_Click(object sender, RoutedEventArgs e)
        {
            _data.Settings.CustomBackgroundColor = string.Empty;
        }

        private void BtnClearOpacity_Click(object sender, RoutedEventArgs e)
        {
            _data.Settings.CustomBackgroundOpacity = 0.8;
            SliderOpacity.Value = 0.8;
        }

        private void TextHotkey_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

            if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin ||
                key == System.Windows.Input.Key.DeadCharProcessed)
            {
                return;
            }

            var modifiers = System.Windows.Input.Keyboard.Modifiers;
            if (modifiers == System.Windows.Input.ModifierKeys.None) return; // Must have at least one modifier

            string hotkeyString = "";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) hotkeyString += "Ctrl+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt)) hotkeyString += "Alt+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift)) hotkeyString += "Shift+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows)) hotkeyString += "Win+";

            hotkeyString += key.ToString();
            
            _data.Settings.GlobalHotkey = hotkeyString;
            TextHotkey.Text = hotkeyString;
        }
    }
}
