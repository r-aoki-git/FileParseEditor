using System.Text;
using FileParseEditor.Core.Models;
using FileParserLib;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// キー一覧の1行（基本設計書 3-4-2）。
/// </summary>
/// <param name="Key">キー名。</param>
/// <param name="ValueText">値の LFC の表記（保存時と同じ）。</param>
/// <param name="TypeText">要素ごとの型を「, 」区切りにしたもの（例：<c>Int, String, Int</c>）。</param>
public sealed record KeyListItem(string Key, string ValueText, string TypeText)
{
    private const string TypeSeparator = ", ";

    /// <summary>キー名と値から一覧の1行を作る。</summary>
    public static KeyListItem From(string key, IReadOnlyList<LfcValue> values)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(values);

        var types = new StringBuilder();
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                types.Append(TypeSeparator);
            }
            types.Append(values[i].Type);
        }
        return new KeyListItem(key, LfcValueNotation.Format(values), types.ToString());
    }
}
