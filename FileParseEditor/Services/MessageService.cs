using System.Windows;
using FileParseEditor.Core.Services;

namespace FileParseEditor.Services;

/// <summary>
/// <see cref="IMessageService"/> の実装。WPF 標準のメッセージボックスを、メイン画面を親として表示する。
/// </summary>
public sealed class MessageService : IMessageService
{
    private const string Caption = "FileParseEditor";

    public void ShowError(string message) => ShowMessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInformation(string message) => ShowMessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool ConfirmYesNo(string message) =>
        ShowMessageBox(message, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public SaveChangesChoice AskSaveChanges(string message) =>
        ShowMessageBox(message, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
        {
            MessageBoxResult.Yes => SaveChangesChoice.Save,
            MessageBoxResult.No => SaveChangesChoice.Discard,
            _ => SaveChangesChoice.Cancel,   // キャンセル、または Esc・閉じるボタンで閉じた場合
        };

    private static MessageBoxResult ShowMessageBox(string message, MessageBoxButton button, MessageBoxImage image)
    {
        var owner = Application.Current?.MainWindow;
        return owner is null
            ? MessageBox.Show(message, Caption, button, image)
            : MessageBox.Show(owner, message, Caption, button, image);
    }
}
