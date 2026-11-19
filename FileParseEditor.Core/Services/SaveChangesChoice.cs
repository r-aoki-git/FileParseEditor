namespace FileParseEditor.Core.Services;

/// <summary>
/// 保存していない変更の確認（M01）での選択（基本設計書 6-10）。
/// </summary>
public enum SaveChangesChoice
{
    /// <summary>はい：保存してから元の操作を続ける。</summary>
    Save,

    /// <summary>いいえ：保存せずに元の操作を続ける。</summary>
    Discard,

    /// <summary>キャンセル：元の操作を中止する。</summary>
    Cancel,
}
