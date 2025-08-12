using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaVncClient.Views.Dialogs
{
    public class EnterCreadentialsDialog : Window
    {
        private TextBox UsernameTextBox => this.FindControl<TextBox>("UsernameTextBox");
        private TextBox PasswordTextBox => this.FindControl<TextBox>("PasswordTextBox");

        public EnterCreadentialsDialog()
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
            Close((UsernameTextBox.Text, PasswordTextBox.Text));
        }
    }
}
