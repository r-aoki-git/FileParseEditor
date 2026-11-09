using FileParseEditor.Core.Services;

namespace FileParseEditor.Tests.Helpers;

/// <summary>
/// テスト用のファイルダイアログ。画面を表示せず、あらかじめ設定したパスを返す。
/// </summary>
internal sealed class FakeFileDialogService : IFileDialogService
{
    /// <summary>ShowOpenDialog が返すパス。null はキャンセルを表す。</summary>
    internal string? OpenResult { get; set; }

    internal int OpenCallCount { get; private set; }

    public string? ShowOpenDialog()
    {
        OpenCallCount++;
        return OpenResult;
    }
}
