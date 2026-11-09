namespace FileParseEditor.Core.Services;

/// <summary>
/// ファイルを開く・保存するダイアログ。ViewModel から画面の部品を直接使わないためのインターフェイス（基本設計書 7-5）。
/// </summary>
public interface IFileDialogService
{
    /// <summary>ファイルを開くダイアログを表示する。</summary>
    /// <returns>選択されたファイルのパス。キャンセルされた場合は null。</returns>
    string? ShowOpenDialog();
}
