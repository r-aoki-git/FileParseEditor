using System.Windows;
using FileParseEditor.Core.Services;
using FileParseEditor.Views;

namespace FileParseEditor.Services;

/// <summary>
/// <see cref="IInputDialogService"/> の実装。セクションの追加・キーの追加ダイアログを、メイン画面を親として表示する。
/// </summary>
public sealed class InputDialogService : IInputDialogService
{
    public string? AskSectionName(string initialName)
    {
        var dialog = new SectionNameDialog(initialName) { Owner = Application.Current?.MainWindow };
        return dialog.ShowDialog() == true ? dialog.SectionName : null;
    }

    public NewKeyInput? AskNewKey(string sectionDisplayName, NewKeyInput initialInput)
    {
        var dialog = new AddKeyDialog(sectionDisplayName, initialInput) { Owner = Application.Current?.MainWindow };
        return dialog.ShowDialog() == true ? dialog.Input : null;
    }
}
