using FileParseEditor.Core.Models;
using FileParserLib;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// 文書の内容から、画面の一覧（セクション・キー・診断情報）の行を作る（基本設計書 3-4）。
/// 行の数は作る前に決まるため、List ではなく配列で返す。
/// </summary>
internal static class ListItemBuilder
{
    /// <summary>セクション一覧の行を作る。グローバルセクションは、文書になくても常に先頭に置く（基本設計書 3-4-1）。</summary>
    internal static SectionListItem[] BuildSectionItems(DocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var names = session.Document.Sections;
        var hasGlobal = session.SectionExists(LfcDocument.GlobalSectionName);
        var items = new SectionListItem[hasGlobal ? names.Count : names.Count + 1];
        items[0] = SectionListItem.ForSection(LfcDocument.GlobalSectionName);

        // ライブラリはグローバルセクションを常に先頭に置くため、残りは登場順のまま後ろに並べる
        var index = 1;
        foreach (var name in names)
        {
            if (name != LfcDocument.GlobalSectionName)
            {
                items[index++] = SectionListItem.ForSection(name);
            }
        }
        return items;
    }

    /// <summary>
    /// セクションのキー一覧の行を作る（基本設計書 3-4-2）。
    /// セクションが文書にない場合（文書にないグローバルセクション）は0件とする。
    /// </summary>
    internal static KeyListItem[] BuildKeyItems(DocumentSession session, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sectionName);

        if (!session.SectionExists(sectionName))
        {
            return Array.Empty<KeyListItem>();
        }

        var document = session.Document;
        var keys = document.Keys(sectionName);
        var items = new KeyListItem[keys.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = KeyListItem.ForKey(keys[i], document.Get(sectionName, keys[i]));
        }
        return items;
    }

    /// <summary>診断情報一覧の行を作る（基本設計書 3-4-5）。</summary>
    internal static DiagnosticListItem[] BuildDiagnosticItems(IReadOnlyList<LfcDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var items = new DiagnosticListItem[diagnostics.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = DiagnosticListItem.ForDiagnostic(diagnostics[i]);
        }
        return items;
    }

    /// <summary>指定した重大度の診断情報の件数を数える（ステータスバー・メッセージに使う）。</summary>
    internal static int CountDiagnostics(IReadOnlyList<LfcDiagnostic> diagnostics, LfcDiagnosticSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var count = 0;
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity == severity)
            {
                count++;
            }
        }
        return count;
    }
}
