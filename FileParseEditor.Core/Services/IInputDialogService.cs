namespace FileParseEditor.Core.Services;

/// <summary>
/// セクションの追加・キーの追加ダイアログ。ViewModel から画面の部品を直接使わないためのインターフェイス（基本設計書 7-5）。
/// </summary>
public interface IInputDialogService
{
    /// <summary>セクションの追加ダイアログを表示する。</summary>
    /// <param name="initialName">入力欄の初期値（入力に問題があって再表示する場合は、前回の入力）。</param>
    /// <returns>入力されたセクション名。キャンセルされた場合は null。</returns>
    string? AskSectionName(string initialName);

    /// <summary>キーの追加ダイアログを表示する。</summary>
    /// <param name="sectionDisplayName">追加先のセクションの表示名。</param>
    /// <param name="initialInput">入力欄の初期値（入力に問題があって再表示する場合は、前回の入力）。</param>
    /// <returns>入力されたキー名と値の表記。キャンセルされた場合は null。</returns>
    NewKeyInput? AskNewKey(string sectionDisplayName, NewKeyInput initialInput);
}
