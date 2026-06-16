using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ClipyFlow.Services
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        // Modifiers:
        // MOD_ALT = 0x0001
        // MOD_CONTROL = 0x0002
        // MOD_SHIFT = 0x0004
        // MOD_WIN = 0x0008
        private const uint MOD_ALT = 0x0001;

        // Virtual Key Codes:
        // V = 0x56
        private const uint VK_V = 0x56;

        private HwndSource? _hwndSource;
        private bool _isRegistered;
        public event EventHandler? HotkeyPressed;

        public void Start(IntPtr windowHandle)
        {
            if (_isRegistered) return;

            _hwndSource = HwndSource.FromHwnd(windowHandle);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(HwndHook);
                bool success = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_ALT, VK_V);
                
                if (success)
                {
                    _isRegistered = true;
                    System.Diagnostics.Debug.WriteLine("Global hotkey Alt+V registered.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to register global hotkey Alt+V.");
                }
            }
        }

        public void Stop()
        {
            if (!_isRegistered || _hwndSource == null) return;

            UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);
            _hwndSource.RemoveHook(HwndHook);
            _hwndSource = null;
            _isRegistered = false;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
