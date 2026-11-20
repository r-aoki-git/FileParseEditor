using FileParserLib;

namespace FileParseEditor.Core.Models;

/// <summary>
/// 値の表記の解釈結果（基本設計書 5-2）。
/// </summary>
internal sealed record ValueNotationResult
{
    private ValueNotationResult(IReadOnlyList<LfcValue> values, IReadOnlyList<string> warnings, ValueNotationError error, string errorDetail)
    {
        Values = values;
        Warnings = warnings;
        Error = error;
        ErrorDetail = errorDetail;
    }

    /// <summary>解釈した値。解釈できなかった場合は0件。</summary>
    internal IReadOnlyList<LfcValue> Values { get; }

    /// <summary>読み込みであれば Warning になる記述のメッセージ（ライブラリの診断情報のメッセージ）。</summary>
    internal IReadOnlyList<string> Warnings { get; }

    /// <summary>解釈できなかった理由。解釈できた場合は <see cref="ValueNotationError.None"/>。</summary>
    internal ValueNotationError Error { get; }

    /// <summary>解釈できなかった理由の詳細（<see cref="ValueNotationError.Syntax"/> の場合のライブラリのメッセージ）。それ以外は空文字列。</summary>
    internal string ErrorDetail { get; }

    internal bool IsSuccess => Error == ValueNotationError.None;

    internal bool HasWarnings => Warnings.Count > 0;

    /// <summary>解釈できた結果を作る。</summary>
    internal static ValueNotationResult CreateSuccess(IReadOnlyList<LfcValue> values, IReadOnlyList<string> warnings) =>
        new(values, warnings, ValueNotationError.None, "");

    /// <summary>解釈できなかった結果を作る。</summary>
    internal static ValueNotationResult CreateFailure(ValueNotationError error, string detail = "") =>
        new(Array.Empty<LfcValue>(), Array.Empty<string>(), error, detail);
}
