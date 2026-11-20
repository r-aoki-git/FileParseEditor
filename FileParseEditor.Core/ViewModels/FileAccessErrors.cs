using System.Security;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// ファイルの読み書きで発生する例外のうち、利用者にメッセージで知らせて操作を続けられるものの判定（基本設計書 9章）。
/// </summary>
internal static class FileAccessErrors
{
    /// <summary>ファイルの読み書きの失敗として、メッセージを表示して操作を続けられる例外か。</summary>
    internal static bool IsRecoverable(Exception exception) =>
        exception is IOException                     // ファイル・フォルダがない、使用中など（FileNotFoundException 等を含む）
            or UnauthorizedAccessException           // アクセス権がない、読み取り専用
            or ArgumentException                     // パスに使えない文字を含む、空のパス
            or NotSupportedException                 // パスの形式が正しくない
            or SecurityException;
}
