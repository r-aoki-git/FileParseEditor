using System.Windows;
using FileParseEditor.Core.Services;

namespace FileParseEditor.Views;

/// <summary>
/// キーの追加ダイアログ（基本設計書 3-5-2）。入力を受け取るだけの画面のため、ViewModel は設けない。
/// </summary>
public partial class AddKeyDialog : Window
{
    public AddKeyDialog(string sectionDisplayName, NewKeyInput initialInput)
    {
        ArgumentNullException.ThrowIfNull(initialInput);

        InitializeComponent();
        SectionText.Text = sectionDisplayName;
        KeyBox.Text = initialInput.Key;
        ValueBox.Text = initialInput.ValueText;
        KeyBox.SelectAll();
    }

    /// <summary>入力されたキー名と値の表記。</summary>
    public NewKeyInput Input => new(KeyBox.Text, ValueBox.Text);

    private void OnOkClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
