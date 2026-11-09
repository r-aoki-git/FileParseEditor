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

    /// <summary>
    /// 値を、保存時と同じ LFC の表記に変換する（基本設計書 5-1）。値なし（Null 1つ）の場合は空文字列。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> が0件（ライブラリの Set の仕様）。</exception>
    public static string Format(IReadOnlyList<LfcValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var document = LfcDocument.CreateEmpty();
        var array = new LfcValue[values.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = values[i];
        }
        document.Set(LfcDocument.GlobalSectionName, WorkKey, array);

        // 書き出し結果は "Value = 1,2\n"、値なしの場合は "Value =\n" の1行になる
        var line = document.ToText();
        var notation = line[KeyPrefix.Length..^1];
        return notation.Length == 0 ? notation : notation[1..];
    }
}
