using FileParserLib;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// セクション一覧の1行（基本設計書 3-4-1）。
/// </summary>
/// <param name="Name">セクション名。グローバルセクションは空文字列。</param>
/// <param name="DisplayName">一覧に表示する名前。</param>
public sealed record SectionListItem(string Name, string DisplayName)
{
    internal const string GlobalDisplayName = "（グローバル）";

    /// <summary>
    /// セクション名から一覧の1行を作る。通常のセクションはファイル上の記法と同じく角かっこを付け、
    /// グローバルセクションは「（グローバル）」と表示する。
    /// </summary>
    public static SectionListItem From(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name == LfcDocument.GlobalSectionName
            ? new SectionListItem(name, GlobalDisplayName)
            : new SectionListItem(name, $"[{name}]");
    }

    public bool IsGlobal => Name == LfcDocument.GlobalSectionName;
}
