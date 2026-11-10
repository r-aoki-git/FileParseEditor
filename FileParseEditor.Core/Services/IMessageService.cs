namespace FileParseEditor.Core.Services;

/// <summary>
/// 確認・エラーのメッセージ。ViewModel から画面の部品を直接使わないためのインターフェイス（基本設計書 7-5）。
/// </summary>
public interface IMessageService
{
    /// <summary>エラーのメッセージを表示する。</summary>
    void ShowError(string message);

    /// <summary>情報のメッセージを表示する。</summary>
    void ShowInformation(string message);
}
