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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var storage = new StorageService();
        var data = storage.LoadData();

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
            
            storage.SaveData(data); // save immediately to prevent asking again
        }

        // Initialize TaskbarIcon
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "ClipyFlow - Smart Clipboard",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/icon.png"))
        };

        // Context menu for the tray icon can be added here
        
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

        // Initialize and start HotkeyManager
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += (s, ev) =>
        {
            _mainWindow.ShowAtCursor();
        };
        _hotkeyManager.Start(helper.Handle);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _clipboardManager?.Dispose();
        _taskbarIcon?.Dispose();
        base.OnExit(e);
    }
}
