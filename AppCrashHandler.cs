using System;
using System.IO;
using System.Windows;

namespace ClipyFlow
{
    public partial class App
    {
        static App()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                File.WriteAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"),
                    $"[{DateTime.Now}] UNHANDLED EXCEPTION:\n{ex}");
            };

            System.Windows.Threading.Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) =>
            {
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt"),
                    $"[{DateTime.Now}] DISPATCHER EXCEPTION:\n{args.Exception}");
                args.Handled = true;
                MessageBox.Show(
                    $"Помилка: {args.Exception.Message}\n\n{args.Exception.InnerException?.Message}\n\nДив. crash.txt",
                    "ClipyFlow - Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };
        }
    }
}
