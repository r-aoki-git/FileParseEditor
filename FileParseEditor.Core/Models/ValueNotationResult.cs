using FileParserLib;

namespace FileParseEditor.Core.Models;

/// <summary>
/// 値の表記の解釈結果（基本設計書 5-2）。
/// </summary>
public sealed record ValueNotationResult
{
    private ValueNotationResult(IReadOnlyList<LfcValue> values, IReadOnlyList<string> warnings, ValueNotationError error, string errorDetail)
    {
        Values = values;
        Warnings = warnings;
        Error = error;
        ErrorDetail = errorDetail;
    }

    /// <summary>解釈した値。解釈できなかった場合は0件。</summary>
    public IReadOnlyList<LfcValue> Values { get; }

    /// <summary>読み込みであれば Warning になる記述のメッセージ（ライブラリの診断情報のメッセージ）。</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>解釈できなかった理由。解釈できた場合は <see cref="ValueNotationError.None"/>。</summary>
    public ValueNotationError Error { get; }

    /// <summary>解釈できなかった理由の詳細（<see cref="ValueNotationError.Syntax"/> の場合のライブラリのメッセージ）。それ以外は空文字列。</summary>
    public string ErrorDetail { get; }

    public bool IsSuccess => Error == ValueNotationError.None;

    public bool HasWarnings => Warnings.Count > 0;

    internal static ValueNotationResult Success(IReadOnlyList<LfcValue> values, IReadOnlyList<string> warnings) =>
        new(values, warnings, ValueNotationError.None, "");

    internal static ValueNotationResult Failure(ValueNotationError error, string detail = "") =>
        new(Array.Empty<LfcValue>(), Array.Empty<string>(), error, detail);
}
