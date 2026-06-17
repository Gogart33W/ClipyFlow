using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using ClipyFlow.Services;

namespace ClipyFlow;

public partial class App : Application
{
    private TaskbarIcon? _taskbarIcon;
    private ClipboardManager? _clipboardManager;
    private HotkeyManager? _hotkeyManager;
    private MainWindow? _mainWindow;
    private static System.Threading.Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "ClipyFlowAppMutex";
        _mutex = new System.Threading.Mutex(true, appName, out bool createdNew);
        if (!createdNew)
        {
            Application.Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        var storage = new StorageService();
        var data = storage.LoadData();
        
        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
            data.Settings.Theme == "Light" ? Wpf.Ui.Appearance.ApplicationTheme.Light :
            data.Settings.Theme == "Dark" ? Wpf.Ui.Appearance.ApplicationTheme.Dark :
            Wpf.Ui.Appearance.ApplicationTheme.Unknown
        );

        if (data.IsFirstRun)
        {
            var result = MessageBox.Show(
                "Додати ClipyFlow в автозавантаження Windows?",
                "ClipyFlow - Перший запуск",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            var autorun = new AutorunService();
            if (result == MessageBoxResult.Yes)
            {
                autorun.SetAutorun(true);
            }
            
            data.IsFirstRun = false;
            _ = storage.SaveDataAsync(data); // save immediately to prevent asking again
        }

        // Initialize TaskbarIcon
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "ClipyFlow - Smart Clipboard",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/icon.png"))
        };

        // Context menu for the tray icon
        var contextMenu = new System.Windows.Controls.ContextMenu();
        
        var showItem = new System.Windows.Controls.MenuItem { Header = "Show ClipyFlow (Alt+V)" };
        showItem.Click += (s, ev) => _mainWindow?.ShowAtCursor(false);
        
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (s, ev) => Application.Current.Shutdown();
        
        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = contextMenu;
        _taskbarIcon.TrayLeftMouseUp += (s, ev) => _mainWindow?.ShowAtCursor(false);

        // Initialize hidden main window
        _mainWindow = new MainWindow(storage, data);
        
        // We need the window handle to register the clipboard hook
        var helper = new WindowInteropHelper(_mainWindow);
        helper.EnsureHandle();

        // Initialize and start ClipboardManager
        _clipboardManager = new ClipboardManager();
        _clipboardManager.ClipboardChanged += (s, item) =>
        {
            _mainWindow.AddItem(item);
        };
        _clipboardManager.StartListening(helper.Handle);

        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += (s, ev) =>
        {
            _mainWindow.ShowAtCursor(true);
        };
        bool hkSuccess = _hotkeyManager.Start(helper.Handle, data.Settings.GlobalHotkey);
        if (!hkSuccess && _taskbarIcon != null)
        {
            _taskbarIcon.ShowBalloonTip("ClipyFlow", "Failed to register Alt+V hotkey. It might be used by another app.", BalloonIcon.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _clipboardManager?.Dispose();
        _taskbarIcon?.Dispose();
        base.OnExit(e);
    }
}
