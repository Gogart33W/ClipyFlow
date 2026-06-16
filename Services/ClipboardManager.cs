using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ClipyFlow.Models;

namespace ClipyFlow.Services
{
    public class ClipboardManager : IDisposable
    {
        private HwndSource? _hwndSource;
        private bool _isListening;

        // Win32 API
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        public event EventHandler<ClipboardItem>? ClipboardChanged;

        public ClipboardManager()
        {
        }

        public void StartListening(IntPtr windowHandle)
        {
            if (_isListening) return;

            _hwndSource = HwndSource.FromHwnd(windowHandle);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(HwndHook);
                AddClipboardFormatListener(windowHandle);
                _isListening = true;
                Debug.WriteLine("Clipboard listener started.");
            }
        }

        public void StopListening()
        {
            if (!_isListening || _hwndSource == null) return;

            RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.RemoveHook(HwndHook);
            _hwndSource = null;
            _isListening = false;
            Debug.WriteLine("Clipboard listener stopped.");
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                OnClipboardChanged();
            }

            return IntPtr.Zero;
        }

        private void OnClipboardChanged()
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    string text = System.Windows.Clipboard.GetText();
                    
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var item = ClipboardItem.Create(text);
                        
                        Debug.WriteLine($"[Clipboard] Copied: {text} | Type: {item.ItemType}");
                        Console.WriteLine($"[Clipboard] Copied: {text} | Type: {item.ItemType}");
                        
                        ClipboardChanged?.Invoke(this, item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading clipboard: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopListening();
        }
    }
}
