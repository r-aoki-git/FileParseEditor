using System.Windows;

namespace FileParseEditor.Views;

/// <summary>
/// セクションの追加ダイアログ（基本設計書 3-5-1）。入力を受け取るだけの画面のため、ViewModel は設けない。
/// </summary>
public partial class SectionNameDialog : Window
{
    public SectionNameDialog(string initialName)
    {
        InitializeComponent();
        NameBox.Text = initialName;
        NameBox.SelectAll();
    }

    /// <summary>入力されたセクション名。</summary>
    public string SectionName => NameBox.Text;

    private void OnOkClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
