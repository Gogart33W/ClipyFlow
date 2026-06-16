using System;
using Microsoft.Win32;

namespace ClipyFlow.Services
{
    public class AutorunService
    {
        private const string AppName = "ClipyFlow";
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public bool IsAutorunEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                if (key != null)
                {
                    var val = key.GetValue(AppName);
                    return val != null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autorun check failed: {ex.Message}");
            }
            return false;
        }

        public void SetAutorun(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                if (key != null)
                {
                    if (enable)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key.SetValue(AppName, $"\"{exePath}\" --background");
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autorun set failed: {ex.Message}");
            }
        }
    }
}
