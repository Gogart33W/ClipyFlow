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
        private bool _ignoreNextClipboardChange = false;

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
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;

        public MainWindow(StorageService storage, AppData data)
        {
            InitializeComponent();
            _storage = storage;

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
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            HideWindow();
        }

        public void AddItem(ClipboardItem item)
        {
            Dispatcher.Invoke(() =>
            {
                if (_ignoreNextClipboardChange)
                {
                    _ignoreNextClipboardChange = false;
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
                History = new System.Collections.Generic.List<ClipboardItem>(History), 
                Snippets = new System.Collections.Generic.List<SnippetItem>(Snippets),
                LibraryCategories = new System.Collections.Generic.List<LanguageCategory>(LibraryCategories)
            };
            _storage.SaveData(data);
        }

        public void ShowAtCursor()
        {
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

        private void HistoryList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is ClipboardItem item)
            {
                CopyToClipboard(item.Text);
                listView.SelectedItem = null;
            }
        }

        private void SnippetsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is SnippetItem item)
            {
                CopyToClipboard(item.Content);
                listView.SelectedItem = null;
            }
        }

        private void LibraryList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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

                CopyToClipboard(item.Content);
                listView.SelectedItem = null;
            }
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.TextBox tb)
            {
                string query = tb.Text.ToLowerInvariant();
                var historyView = System.Windows.Data.CollectionViewSource.GetDefaultView(History);
                historyView.Filter = item => 
                {
                    if (string.IsNullOrWhiteSpace(query)) return true;
                    if (item is ClipboardItem c) return c.Text.ToLowerInvariant().Contains(query);
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
        }

        private async void CopyToClipboard(string text)
        {
            try
            {
                _ignoreNextClipboardChange = true;
                System.Windows.Clipboard.SetText(text);
                HideWindow();

                // Small delay to allow the previous window to regain focus
                await System.Threading.Tasks.Task.Delay(150);

                // Simulate Ctrl+V to auto-paste into active window
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Copy failed: {ex.Message}");
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            History.Clear();
            SaveData();
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
            }
        }

        private void NavHistory_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Visible;
            ViewSnippets.Visibility = Visibility.Collapsed;
            ViewLibrary.Visibility = Visibility.Collapsed;
            BtnClearHistory.Visibility = Visibility.Visible;

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
        }

        private void NavSnippets_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Collapsed;
            ViewSnippets.Visibility = Visibility.Visible;
            ViewLibrary.Visibility = Visibility.Collapsed;
            BtnClearHistory.Visibility = Visibility.Collapsed; // Hide clear on snippets

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
        }

        private void NavLibrary_Click(object sender, RoutedEventArgs e)
        {
            ViewHistory.Visibility = Visibility.Collapsed;
            ViewSnippets.Visibility = Visibility.Collapsed;
            ViewLibrary.Visibility = Visibility.Visible;
            BtnClearHistory.Visibility = Visibility.Collapsed; // Hide clear on library

            BtnNavHistory.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavSnippets.Appearance = Wpf.Ui.Controls.ControlAppearance.Transparent;
            BtnNavLibrary.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
        }
    }
}