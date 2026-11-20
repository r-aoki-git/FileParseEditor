using FileParseEditor.Core.Services;

namespace FileParseEditor.Tests.Helpers;

/// <summary>
/// テスト用のファイルダイアログ。画面を表示せず、あらかじめ設定したパスを返し、表示した回数と初期値を記録する。
/// </summary>
internal sealed class FakeFileDialogService : IFileDialogService
{
    /// <summary>ShowOpenFileDialog が返すパス。null はキャンセルを表す。</summary>
    internal string? OpenFileDialogResult { get; set; }

    internal int OpenFileDialogCallCount { get; private set; }

    /// <summary>ShowSaveFileDialog が返すパス。null はキャンセルを表す。</summary>
    internal string? SaveFileDialogResult { get; set; }

    /// <summary>ShowSaveFileDialog を表示したときの初期値（表示した回数分）。</summary>
    internal List<string> SaveFileDialogInitialPaths { get; } = new();

    public string? ShowOpenFileDialog()
    {
        OpenFileDialogCallCount++;
        return OpenFileDialogResult;
    }

    public string? ShowSaveFileDialog(string initialPath)
    {
        SaveFileDialogInitialPaths.Add(initialPath);
        return SaveFileDialogResult;
    }
}
