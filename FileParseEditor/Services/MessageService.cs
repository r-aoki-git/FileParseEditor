using System.Windows;
using FileParseEditor.Core.Services;

namespace FileParseEditor.Services;

/// <summary>
/// <see cref="IMessageService"/> の実装。WPF 標準のメッセージボックスを、メイン画面を親として表示する。
/// </summary>
public sealed class MessageService : IMessageService
{
    private const string Caption = "FileParseEditor";

    public void ShowError(string message) => Show(message, MessageBoxImage.Error);

    public void ShowInformation(string message) => Show(message, MessageBoxImage.Information);

    private static void Show(string message, MessageBoxImage image)
    {
        var owner = Application.Current?.MainWindow;
        if (owner is null)
        {
            MessageBox.Show(message, Caption, MessageBoxButton.OK, image);
        }
        else
        {
            MessageBox.Show(owner, message, Caption, MessageBoxButton.OK, image);
        }
    }
}
