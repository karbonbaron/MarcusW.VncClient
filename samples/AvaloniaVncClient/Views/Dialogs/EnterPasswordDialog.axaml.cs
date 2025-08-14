using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaVncClient.Views.Dialogs
{
    public partial class EnterPasswordDialog : Window
    {
        private TextBox _PasswordTextBox => this.FindControl<TextBox>("PasswordTextBox");

        public EnterPasswordDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(null);
        }

        public void OnOkClick(object sender, RoutedEventArgs e)
        {
            Close(_PasswordTextBox.Text);
        }
    }
}
