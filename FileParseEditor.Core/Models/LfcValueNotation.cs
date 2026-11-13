using FileParserLib;

namespace FileParseEditor.Core.Models;

/// <summary>
/// 値（<see cref="LfcValue"/> の一覧）と、LFC の表記（例：<c>1,"tasiro",2</c>）の変換（基本設計書 5章）。
/// ライブラリの型判定・書き出しのクラスは非公開のため、公開 API の <see cref="LfcDocument"/> を使って変換する。
/// </summary>
public static class LfcValueNotation
{
    // 作業用の文書で使うキー名。書き出したテキストからは取り除くため、利用者には見えない
    private const string WorkKey = "Value";

    // 作業用の文書を書き出したときの、値の前に付く部分（"Value =" の後に、値があれば空白1つ）
    private const string KeyPrefix = WorkKey + " =";

    // 入力に Error があっても診断情報として受け取るため、例外を送出させずに解釈する
    private static readonly LfcLoadOptions ParseOptions = new() { ThrowOnError = false };

    /// <summary>
    /// 値を、保存時と同じ LFC の表記に変換する（基本設計書 5-1）。値なし（Null 1つ）の場合は空文字列。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> が0件（ライブラリの Set の仕様）。</exception>
    public static string Format(IReadOnlyList<LfcValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        // 書き出し結果は "Value = 1,2\n"、値なしの場合は "Value =\n" の1行になる
        var line = FormatLine(values);
        var notation = line[KeyPrefix.Length..^1];
        return notation.Length == 0 ? notation : notation[1..];
    }

    /// <summary>
    /// 入力された表記を、ライブラリの読み込みと同じ規則で値に解釈する（基本設計書 5-2）。
    /// 空の入力（空白だけを含む）は値なし（Null 1つ）になる。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> が null。</exception>
    public static ValueNotationResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // 1. 改行があると、作業用の文書の中で別の行（別のキーやセクション）として扱われてしまう
        if (text.Contains('\r') || text.Contains('\n'))
        {
            return ValueNotationResult.Failure(ValueNotationError.LineBreak);
        }

        var document = LfcDocument.Parse($"{KeyPrefix} {text}", ParseOptions);

        // 2. 読み込みであれば Error になる記述
        var warnings = new List<string>();   // 件数は解釈してみるまで分からないため List とする
        foreach (var diagnostic in document.Diagnostics)
        {
            if (diagnostic.Severity == LfcDiagnosticSeverity.Error)
            {
                return ValueNotationResult.Failure(ValueNotationError.Syntax, diagnostic.Message);
            }
            warnings.Add(diagnostic.Message);
        }

        // Error がなければキーの行は必ず読み込まれるが、念のため確認する
        if (!document.TryGet(LfcDocument.GlobalSectionName, WorkKey, out var values))
        {
            return ValueNotationResult.Failure(ValueNotationError.Syntax);
        }

        // 3. クォートの外の # はコメントとして扱われ、値だけを書き出した結果との差になって現れる
        if (document.ToText() != FormatLine(values))
        {
            return ValueNotationResult.Failure(ValueNotationError.Comment);
        }

        // 4・5. 成功（Warning があれば、呼び出し側で確認する）
        return ValueNotationResult.Success(values, warnings.ToArray());
    }

    /// <summary>作業用の文書に値だけを設定し、書き出した1行（末尾の改行を含む）を返す。</summary>
    private static string FormatLine(IReadOnlyList<LfcValue> values)
    {
        var document = LfcDocument.CreateEmpty();
        var array = new LfcValue[values.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = values[i];
        }
        document.Set(LfcDocument.GlobalSectionName, WorkKey, array);
        return document.ToText();
    }
}
