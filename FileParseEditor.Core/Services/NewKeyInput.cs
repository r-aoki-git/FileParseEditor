namespace FileParseEditor.Core.Services;

/// <summary>
/// キーの追加ダイアログの入力（基本設計書 3-5-2）。
/// </summary>
/// <param name="Key">キー名。</param>
/// <param name="ValueText">値の表記（空欄は値なし）。</param>
public sealed record NewKeyInput(string Key, string ValueText)
{
    /// <summary>何も入力していない状態。</summary>
    public static NewKeyInput Empty { get; } = new("", "");
}
