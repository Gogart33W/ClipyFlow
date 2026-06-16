using System.Windows;
using System.Windows.Input;

namespace ClipyFlow.Views
{
    public partial class InputDialog : Wpf.Ui.Controls.FluentWindow
    {
        public string InputText => InputBox.Text;
        public string WindowTitle { get; set; }

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            WindowTitle = title;
            PromptText.Text = prompt;
            DataContext = this;
            Loaded += (_, _) => InputBox.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) DialogResult = true;
            else if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
