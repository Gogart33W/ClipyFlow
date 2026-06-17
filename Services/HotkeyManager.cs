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

        private void ParseHotkey(string hotkeyStr, out uint modifiers, out uint vk)
        {
            modifiers = 0;
            vk = 0;
            if (string.IsNullOrEmpty(hotkeyStr)) return;

            if (hotkeyStr.Contains("Ctrl+")) modifiers |= 0x0002; // MOD_CONTROL
            if (hotkeyStr.Contains("Alt+")) modifiers |= 0x0001; // MOD_ALT
            if (hotkeyStr.Contains("Shift+")) modifiers |= 0x0004; // MOD_SHIFT
            if (hotkeyStr.Contains("Win+")) modifiers |= 0x0008; // MOD_WIN

            string keyPart = hotkeyStr.Contains("+") ? hotkeyStr.Substring(hotkeyStr.LastIndexOf('+') + 1) : hotkeyStr;
            if (Enum.TryParse<System.Windows.Input.Key>(keyPart, true, out var key))
            {
                vk = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
            }
        }

        public bool Start(IntPtr windowHandle, string hotkeyStr)
        {
            if (_isRegistered) return true;

            _hwndSource = HwndSource.FromHwnd(windowHandle);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(HwndHook);
                
                ParseHotkey(hotkeyStr, out uint modifiers, out uint vk);
                if (vk == 0) // Fallback to Alt+V
                {
                    modifiers = MOD_ALT;
                    vk = VK_V;
                }

                bool success = RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, vk);
                
                if (success)
                {
                    _isRegistered = true;
                    System.Diagnostics.Debug.WriteLine($"Global hotkey {hotkeyStr} registered.");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to register global hotkey. It might be in use.");
                    return false;
                }
            }
            return false;
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
