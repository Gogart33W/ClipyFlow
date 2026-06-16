using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ClipyFlow.Models;
using ClipyFlow.Services;

using Wpf.Ui.Controls;

namespace ClipyFlow
{
    public partial class MainWindow : FluentWindow
    {
        public ObservableCollection<ClipboardItem> History { get; set; } = new ObservableCollection<ClipboardItem>();
        public ObservableCollection<SnippetItem> Snippets { get; set; } = new ObservableCollection<SnippetItem>();
        public ObservableCollection<LanguageCategory> LibraryCategories { get; set; } = new ObservableCollection<LanguageCategory>();
        private readonly StorageService _storage;
        private readonly AppData _data;
        private bool _isInternalAction = false;
        private bool _ignoreNextImage = false;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, [In, Out] MONITORINFO lpmi);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
            public static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public UIntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_V = 0x56;

        private string? _lastCopiedText = null;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private IntPtr _previousForegroundWindow;

        public MainWindow(StorageService storage, AppData data)
        {
            InitializeComponent();
            _storage = storage;
            _data = data;

            // Load data (limit to 50 items to prevent huge memory usage if JSON was modified)
            foreach (var item in data.History.Take(50))
            {
                History.Add(item);
            }
            foreach (var item in data.Snippets)
            {
                Snippets.Add(item);
            }

            // Sync IsPinned for history items
            foreach (var item in History)
            {
                item.IsPinned = Snippets.Any(s => s.Content == item.Text);
            }

            foreach (var cat in data.LibraryCategories.OrderByDescending(x => x.TotalUsageCount))
            {
                // Re-sort inner snippets
                var sortedSnippets = cat.Snippets.OrderByDescending(s => s.UsageCount).ToList();
                cat.Snippets.Clear();
                foreach (var s in sortedSnippets) cat.Snippets.Add(s);

                LibraryCategories.Add(cat);
            }

            DataContext = this;
            
            var historyView = System.Windows.Data.CollectionViewSource.GetDefaultView(History);
            historyView.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("TimeGroup"));
            
            // Apply customization
            TogglePinWindow.IsChecked = _data.Settings.IsWindowPinned;
            TogglePinWindow.Click += (s, e) => 
            {
                _data.Settings.IsWindowPinned = TogglePinWindow.IsChecked == true;
                SaveData();
            };
            
            UpdateCustomBackground();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            if (!_isInternalAction && !_data.Settings.IsWindowPinned)
            {
                HideWindow();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            HideWindow();
        }

        public void UpdateCustomBackground()
        {
            // Apply Opacity
            CustomBgImage.Opacity = _data.Settings.CustomBackgroundOpacity;

            // Apply Background Image
            if (!string.IsNullOrEmpty(_data.Settings.CustomBackgroundPath) && System.IO.File.Exists(_data.Settings.CustomBackgroundPath))
            {
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(_data.Settings.CustomBackgroundPath);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    CustomBgImage.Source = bmp;
                }
                catch { CustomBgImage.Source = null; }
            }
            else
            {
                CustomBgImage.Source = null;
            }

            // Apply Background Color
            if (!string.IsNullOrEmpty(_data.Settings.CustomBackgroundColor))
            {
                try
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_data.Settings.CustomBackgroundColor);
                    CustomBgColor.Background = new System.Windows.Media.SolidColorBrush(color);
                }
                catch { CustomBgColor.Background = System.Windows.Media.Brushes.Transparent; }
            }
            else
            {
                CustomBgColor.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        public void AddItem(ClipboardItem item)
        {
            Dispatcher.Invoke(() =>
            {
                if (_lastCopiedText == item.Text && item.ItemType != ClipboardItemType.Image)
                {
                    // Ignore self-copy to prevent loops
                    _lastCopiedText = null;
                    return;
                }
                
                if (item.ItemType == ClipboardItemType.Image && _ignoreNextImage)
                {
                    _ignoreNextImage = false;
                    return;
                }

                // Check for duplicate
                var existing = History.FirstOrDefault(x => x.Text == item.Text);
                if (existing != null)
                {
                    History.Remove(existing);
                }

                // Sync pin state
                item.IsPinned = Snippets.Any(s => s.Content == item.Text);

                // Insert at top
                History.Insert(0, item);
                if (History.Count > 50) // limit history
                {
                    History.RemoveAt(History.Count - 1);
                }
                
                SaveData();
            });
        }

        private void SaveData()
        {
            var data = new AppData
            {
                History = History.ToList(),
                Snippets = Snippets.ToList(),
                LibraryCategories = LibraryCategories.ToList(),
                Settings = _data.Settings
            };
            _ = _storage.SaveDataAsync(data);
        }

        public void ShowAtCursor()
        {
            _previousForegroundWindow = GetForegroundWindow();
            this.Show(); // Show first to ensure PresentationSource is valid
            this.Activate();

            if (GetCursorPos(out POINT p))
            {
                var source = PresentationSource.FromVisual(this);
                double targetX = p.X;
                double targetY = p.Y;

                if (source?.CompositionTarget != null)
                {
                    var matrix = source.CompositionTarget.TransformFromDevice;
                    var wpfPoint = matrix.Transform(new Point(p.X, p.Y));
                    targetX = wpfPoint.X;
                    targetY = wpfPoint.Y;
                }

                // Monitor boundary check
                IntPtr monitor = MonitorFromPoint(p, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var info = new MONITORINFO();
                    if (GetMonitorInfo(monitor, info))
                    {
                        var rect = info.rcWork;
                        
                        // Convert RECT bounds if DPI scaling is present
                        double workRight = rect.right;
                        double workBottom = rect.bottom;

                        if (source?.CompositionTarget != null)
                        {
                            var matrix = source.CompositionTarget.TransformFromDevice;
                            var wpfBottomRight = matrix.Transform(new Point(rect.right, rect.bottom));
                            workRight = wpfBottomRight.X;
                            workBottom = wpfBottomRight.Y;
                        }

                        // Adjust if it goes off bottom
                        if (targetY + this.Height > workBottom)
                        {
                            targetY = targetY - this.Height; // Spawn above cursor
                        }

                        // Adjust if it goes off right
                        if (targetX + this.Width > workRight)
                        {
                            targetX = targetX - this.Width; // Spawn left of cursor
                        }
                    }
                }

                this.Left = targetX;
                this.Top = targetY;
            }

            InputSearch.Focus();
        }



        private void HideWindow()
        {
            this.Hide();
            InputSearch.Text = string.Empty; // Clear search for next time
        }

        private void InputSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var activeListView = GetActiveListView();
            if (activeListView == null) return;

            if (e.Key == Key.Down)
            {
                e.Handled = true;
                if (activeListView.Items.Count > 0)
                {
                    activeListView.SelectedIndex = Math.Min(activeListView.Items.Count - 1, activeListView.SelectedIndex + 1);
                    activeListView.ScrollIntoView(activeListView.SelectedItem);
                }
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                if (activeListView.Items.Count > 0)
                {
                    activeListView.SelectedIndex = Math.Max(0, activeListView.SelectedIndex - 1);
                    activeListView.ScrollIntoView(activeListView.SelectedItem);
                }
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                
                // If nothing is selected, try to select the first item
                if (activeListView.SelectedItem == null && activeListView.Items.Count > 0)
                {
                    activeListView.SelectedIndex = 0;
                }

                if (activeListView.SelectedItem != null)
                {
                    if (activeListView.SelectedItem is ClipboardItem clipboardItem) CopyToClipboardItem(clipboardItem);
                    else if (activeListView.SelectedItem is SnippetItem snippetItem) CopyToClipboardText(snippetItem.Content);
                    else if (activeListView.SelectedItem is LanguageSnippet langSnippet) CopyToClipboardText(langSnippet.Content);
                }
            }
        }

        private System.Windows.Controls.ListView? GetActiveListView()
        {
            if (ViewHistory.Visibility == Visibility.Visible) return ViewHistory;
            if (ViewSnippets.Visibility == Visibility.Visible) return ViewSnippets;
            if (ViewLibrary.Visibility == Visibility.Visible)
            {
                // Find the active ListView in the TabControl
                if (ViewLibrary.ItemContainerGenerator.ContainerFromItem(ViewLibrary.SelectedItem) is System.Windows.Controls.TabItem tabItem)
                {
                    return FindVisualChild<System.Windows.Controls.ListView>(tabItem);
                }
            }
            return null;
        }

        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private static T? FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                HideWindow();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideWindow();
        }

        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ClipboardItem item)
            {
                CopyToClipboardItem(item);
                ViewHistory.SelectedItem = null;
            }
        }

        private void SnippetsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && FindVisualParent<System.Windows.Controls.ListViewItem>(source) == null)
            {
                return;
            }

            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is SnippetItem item)
            {
                if (item.IsEditing) return; // Prevent copying if double-clicked to edit
                CopyToClipboardText(item.Content);
                listView.SelectedItem = null;
            }
        }

        private void LibraryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is LanguageSnippet item)
            {
                item.UsageCount++;
                
                var parentCategory = LibraryCategories.FirstOrDefault(c => c.Snippets.Contains(item));
                if (parentCategory != null)
                {
                    parentCategory.TotalUsageCount++;
                    
                    // Resort snippets
                    var sortedSnippets = parentCategory.Snippets.OrderByDescending(s => s.UsageCount).ToList();
                    parentCategory.Snippets.Clear();
                    foreach (var s in sortedSnippets) parentCategory.Snippets.Add(s);
                }

                // Resort categories
                var sortedCategories = LibraryCategories.OrderByDescending(c => c.TotalUsageCount).ToList();
                LibraryCategories.Clear();
                foreach (var cat in sortedCategories) LibraryCategories.Add(cat);

                SaveData();

                CopyToClipboardText(item.Content);
                listView.SelectedItem = null;
            }
        }

        private System.Windows.Threading.DispatcherTimer? _searchTimer;

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_searchTimer == null)
            {
                _searchTimer = new System.Windows.Threading.DispatcherTimer();
                _searchTimer.Interval = TimeSpan.FromMilliseconds(150);
                _searchTimer.Tick += (s, ev) => 
                {
                    _searchTimer.Stop();
                    ApplySearchFilter();
                };
            }
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void ApplySearchFilter()
        {
            string query = InputSearch.Text.ToLowerInvariant();
            var historyView = System.Windows.Data.CollectionViewSource.GetDefaultView(History);
            historyView.Filter = item => 
            {
                if (string.IsNullOrWhiteSpace(query)) return true;
                if (item is ClipboardItem c) 
                {
                    if (c.ItemType == ClipboardItemType.Image && query != "image") return false;
                    return c.Text.ToLowerInvariant().Contains(query);
                }
                return false;
            };

            var snippetsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Snippets);
            snippetsView.Filter = item => 
            {
                if (string.IsNullOrWhiteSpace(query)) return true;
                if (item is SnippetItem s) return s.Title.ToLowerInvariant().Contains(query) || s.Content.ToLowerInvariant().Contains(query);
                return false;
            };
        }

        private void CopyToClipboardItem(ClipboardItem item)
        {
            if (item.ItemType == ClipboardItemType.Image && item.ImagePath != null)
            {
                try
                {
                    _lastCopiedText = null;
                    _ignoreNextImage = true;
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(item.ImagePath);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    System.Windows.Clipboard.SetImage(bmp);
                    PerformAutoPaste();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Image copy failed: {ex.Message}");
                }
            }
            else
            {
                CopyToClipboardText(item.Text);
            }
        }

        private void CopyToClipboardText(string text)
        {
            try
            {
                _lastCopiedText = text;
                System.Windows.Clipboard.SetText(text);
                PerformAutoPaste();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Text copy failed: {ex.Message}");
            }
        }

        private async void PerformAutoPaste()
        {
            try
            {
                if (!_data.Settings.IsWindowPinned)
                {
                    HideWindow();
                }
                
                if (!_data.Settings.AutoPasteEnabled) return;

                // If window is pinned, we shouldn't steal focus from ourselves with Ctrl+V because it will paste into ClipyFlow's search box.
                if (_data.Settings.IsWindowPinned)
                {
                    if (_previousForegroundWindow != IntPtr.Zero)
                    {
                        SetForegroundWindow(_previousForegroundWindow);
                        await System.Threading.Tasks.Task.Delay(50);
                    }
                    else
                    {
                        return; // Nowhere to paste
                    }
                }

                // Small delay to allow the previous window to regain focus
                await System.Threading.Tasks.Task.Delay(150);

                // Simulate Ctrl+V using SendInput
                INPUT[] inputs = new INPUT[4];

                // Ctrl Down
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].U.ki.wVk = VK_CONTROL;

                // V Down
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].U.ki.wVk = VK_V;

                // V Up
                inputs[2].type = INPUT_KEYBOARD;
                inputs[2].U.ki.wVk = VK_V;
                inputs[2].U.ki.dwFlags = KEYEVENTF_KEYUP;

                // Ctrl Up
                inputs[3].type = INPUT_KEYBOARD;
                inputs[3].U.ki.wVk = VK_CONTROL;
                inputs[3].U.ki.dwFlags = KEYEVENTF_KEYUP;

                SendInput((uint)inputs.Length, inputs, INPUT.Size);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-paste failed: {ex.Message}");
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _isInternalAction = true;
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Are you sure you want to clear all history?", 
                    "Clear History", 
                    System.Windows.MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                    
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    History.Clear();
                    SaveData();
                }
            }
            finally
            {
                _isInternalAction = false;
            }
        }

        private void DeleteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ClipboardItem item)
            {
                History.Remove(item);
                SaveData();
                e.Handled = true;
            }
        }

        private void SnippetText_DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SnippetItem item)
            {
                item.IsEditing = true;
                e.Handled = true;
            }
        }

        private void SaveSnippet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is SnippetItem item)
            {
                item.IsEditing = false;
                SaveData();
            }
        }

        private void PinHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ClipboardItem item)
            {
                var existingSnippet = Snippets.FirstOrDefault(s => s.Content == item.Text);
                if (existingSnippet != null)
                {
                    // Already pinned, unpin it
                    Snippets.Remove(existingSnippet);
                    item.IsPinned = false;
                }
                else
                {
                    // Pin it
                    string title = item.Text.Length > 20 ? item.Text.Substring(0, 20).Replace("\n", " ") + "..." : item.Text.Replace("\n", " ");
                    Snippets.Add(new SnippetItem { Title = title, Content = item.Text });
                    item.IsPinned = true;
                }
                SaveData();
                e.Handled = true;
            }
        }

        private void UnpinSnippet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is SnippetItem item)
            {
                Snippets.Remove(item);
                
                // Update history if exists
                var historyItem = History.FirstOrDefault(h => h.Text == item.Content);
                if (historyItem != null)
                {
                    historyItem.IsPinned = false;
                }
                
                SaveData();
                e.Handled = true;
            }
        }

        private void NavHistory_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Visible;
            ViewSnippets.Visibility = Visibility.Collapsed;
            ViewLibrary.Visibility = Visibility.Collapsed;
            ViewEmoji.Visibility = Visibility.Collapsed;
            ViewGif.Visibility = Visibility.Collapsed;
            BtnClearHistory.Visibility = Visibility.Visible;

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavEmoji.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavGif.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
        }

        private void NavSnippets_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Collapsed;
            ViewSnippets.Visibility = Visibility.Visible;
            ViewLibrary.Visibility = Visibility.Collapsed;
            ViewEmoji.Visibility = Visibility.Collapsed;
            ViewGif.Visibility = Visibility.Collapsed;
            BtnClearHistory.Visibility = Visibility.Collapsed; // Hide clear on snippets

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavEmoji.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavGif.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
        }

        private void NavLibrary_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Collapsed;
            ViewSnippets.Visibility = Visibility.Collapsed;
            ViewLibrary.Visibility = Visibility.Visible;
            ViewEmoji.Visibility = Visibility.Collapsed;
            ViewGif.Visibility = Visibility.Collapsed;
            BtnClearHistory.Visibility = Visibility.Collapsed; // Hide clear on library

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            BtnNavEmoji.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavGif.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
        }

        private void NavEmoji_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Collapsed;
            ViewSnippets.Visibility = Visibility.Collapsed;
            ViewLibrary.Visibility = Visibility.Collapsed;
            ViewEmoji.Visibility = Visibility.Visible;
            ViewGif.Visibility = Visibility.Collapsed;
            BtnClearHistory.Visibility = Visibility.Collapsed; // Hide clear on emoji

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavEmoji.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            BtnNavGif.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
        }

        private void EmojiControl_EmojiSelected(object sender, Models.EmojiItem e)
        {
            CopyToClipboardText(e.Character);
        }

        private void NavGif_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Collapsed;
            ViewSnippets.Visibility = Visibility.Collapsed;
            ViewLibrary.Visibility = Visibility.Collapsed;
            ViewEmoji.Visibility = Visibility.Collapsed;
            ViewGif.Visibility = Visibility.Visible;
            BtnClearHistory.Visibility = Visibility.Collapsed; // Hide clear on gif

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavEmoji.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavGif.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            
            ViewGif.Initialize(_data.Settings.TenorApiKey);
        }

        private async void GifControl_GifSelected(object sender, string gifUrl)
        {
            try
            {
                var client = new System.Net.Http.HttpClient();
                var data = await client.GetByteArrayAsync(gifUrl);
                
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string imagesDir = System.IO.Path.Combine(appData, "ClipyFlow", "images");
                if (!System.IO.Directory.Exists(imagesDir)) System.IO.Directory.CreateDirectory(imagesDir);
                
                string fileName = $"gif_{DateTime.Now:yyyyMMdd_HHmmss_fff}.gif";
                string fullPath = System.IO.Path.Combine(imagesDir, fileName);
                System.IO.File.WriteAllBytes(fullPath, data);

                // Cleanup old gifs (keep last 20)
                var dir = new System.IO.DirectoryInfo(imagesDir);
                var files = dir.GetFiles("gif_*.gif").OrderByDescending(f => f.CreationTime).ToList();
                if (files.Count > 20)
                {
                    foreach (var file in files.Skip(20))
                    {
                        try { file.Delete(); } catch { }
                    }
                }

                var dropList = new System.Collections.Specialized.StringCollection { fullPath };
                Clipboard.SetFileDropList(dropList);
                
                // Add to history
                History.Insert(0, new ClipboardItem
                {
                    Text = "Downloaded GIF",
                    ItemType = ClipboardItemType.Image,
                    CopiedAt = DateTime.Now,
                    ImagePath = fullPath
                });
                if (History.Count > 100) History.RemoveAt(History.Count - 1);
                SaveData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error downloading GIF: " + ex.Message);
                CopyToClipboardText(gifUrl); // fallback
            }
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            _isInternalAction = true;
            var settingsWin = new Views.SettingsWindow(_storage, _data);
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
            _isInternalAction = false;
        }
    }
}