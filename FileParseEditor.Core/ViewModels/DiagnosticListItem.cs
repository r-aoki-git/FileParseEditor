using FileParserLib;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// 診断情報一覧の1行（基本設計書 3-4-5）。
/// </summary>
/// <param name="SeverityText">重大度の表示名（「警告」／「エラー」）。</param>
/// <param name="Line">行番号。</param>
/// <param name="Message">メッセージ。</param>
public sealed record DiagnosticListItem(string SeverityText, int Line, string Message)
{
    internal const string WarningText = "警告";
    internal const string ErrorText = "エラー";

    /// <summary>ライブラリの診断情報から一覧の1行を作る。</summary>
    public static DiagnosticListItem From(LfcDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        var severityText = diagnostic.Severity == LfcDiagnosticSeverity.Error ? ErrorText : WarningText;
        return new DiagnosticListItem(severityText, diagnostic.Line, diagnostic.Message);
    }
}
