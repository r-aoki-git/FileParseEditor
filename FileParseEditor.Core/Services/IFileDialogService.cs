namespace FileParseEditor.Core.Services;

/// <summary>
/// ファイルを開く・保存するダイアログ。ViewModel から画面の部品を直接使わないためのインターフェイス（基本設計書 7-5）。
/// </summary>
public interface IFileDialogService
{
    /// <summary>ファイルを開くダイアログを表示する。</summary>
    /// <returns>選択されたファイルのパス。キャンセルされた場合は null。</returns>
    string? ShowOpenFileDialog();

    /// <summary>ファイルを保存するダイアログを表示する（既存のファイルを選んだ場合の上書きの確認は、ダイアログが行う）。</summary>
    /// <param name="initialPath">初期値とするファイルのパス、またはファイル名（新規作成した文書は「無題.lfc」）。</param>
    /// <returns>保存先のパス。キャンセルされた場合は null。</returns>
    string? ShowSaveFileDialog(string initialPath);
}
