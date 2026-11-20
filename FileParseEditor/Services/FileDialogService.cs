using System.IO;
using FileParseEditor.Core.Services;
using Microsoft.Win32;

namespace FileParseEditor.Services;

/// <summary>
/// <see cref="IFileDialogService"/> の実装。WPF 標準のファイルダイアログを表示する。
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    private const string Filter = "LFCファイル (*.lfc)|*.lfc|すべてのファイル (*.*)|*.*";

    public string? ShowOpenFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "開く",
            Filter = Filter,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowSaveFileDialog(string initialPath)
    {
        // 既存のファイルを選んだ場合の上書きの確認は、ダイアログの標準機能（OverwritePrompt）で行う（基本設計書 6-8）
        var dialog = new SaveFileDialog
        {
            Title = "名前を付けて保存",
            Filter = Filter,
            DefaultExt = ".lfc",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = Path.GetFileName(initialPath),
        };

        var directory = Path.GetDirectoryName(initialPath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            dialog.InitialDirectory = directory;
        }
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
