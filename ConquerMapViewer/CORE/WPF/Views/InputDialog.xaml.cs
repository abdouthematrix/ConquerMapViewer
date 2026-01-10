using System.Windows;

namespace ConquerMapViewer.WPF.Views;

public partial class InputDialog : Window
{
    public string InputText { get; private set; } = string.Empty;

    public InputDialog(string message, string title, string defaultValue)
    {
        InitializeComponent();
        Title = title;
        MessageTextBlock.Text = message;
        InputTextBox.Text = defaultValue;
        InputText = defaultValue;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        InputText = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }
}
