using FileParseEditor.Core.Services;
using Microsoft.Win32;

namespace FileParseEditor.Services;

/// <summary>
/// <see cref="IFileDialogService"/> の実装。WPF 標準のファイルダイアログを表示する。
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    private const string Filter = "LFCファイル (*.lfc)|*.lfc|すべてのファイル (*.*)|*.*";

    public string? ShowOpenDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "開く",
            Filter = Filter,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
