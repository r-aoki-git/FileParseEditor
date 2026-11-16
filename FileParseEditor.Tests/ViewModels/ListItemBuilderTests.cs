using FileParseEditor.Core.Models;
using FileParseEditor.Core.ViewModels;
using FileParseEditor.Tests.Helpers;
using FileParserLib;

namespace FileParseEditor.Tests.ViewModels;

/// <summary>ListItemBuilder：文書から一覧の行を作るテスト（基本設計書 3-4）。</summary>
public class ListItemBuilderTests
{
    [Fact]
    public async Task Sections_WithoutGlobalSection_GlobalIsAddedFirst()
    {
        var session = await OpenAsync("[A]\nK = 1\n[B]\n");

        Assert.Equal(new[] { "（グローバル）", "[A]", "[B]" }, ListItemBuilder.BuildSectionItems(session).Select(s => s.DisplayName));
    }

    [Fact]
    public async Task Sections_WithGlobalSection_GlobalIsNotDuplicated()
    {
        var session = await OpenAsync("G = 1\n[A]\n");

        Assert.Equal(new[] { "（グローバル）", "[A]" }, ListItemBuilder.BuildSectionItems(session).Select(s => s.DisplayName));
    }

    [Fact]
    public void Sections_EmptyDocument_OnlyGlobal()
    {
        Assert.Equal(new[] { SectionListItem.ForSection("") }, ListItemBuilder.BuildSectionItems(new DocumentSession()));
    }

    [Fact]
    public async Task Keys_InOrder_WithNotationAndTypes()
    {
        var session = await OpenAsync("[A]\nB = 1,\"x\"\nA =\n");

        Assert.Equal(
            new[] { new KeyListItem("B", "1,\"x\"", "Int, String"), new KeyListItem("A", "", "Null") },
            ListItemBuilder.BuildKeyItems(session, "A"));
    }

    [Fact]
    public async Task Keys_SectionNotInDocument_IsEmpty()
    {
        var session = await OpenAsync("[A]\nK = 1\n");

        Assert.Empty(ListItemBuilder.BuildKeyItems(session, LfcDocument.GlobalSectionName));
        Assert.Empty(ListItemBuilder.BuildKeyItems(session, "Missing"));
    }

    [Fact]
    public void Diagnostics_AndCounts()
    {
        var diagnostics = new[]
        {
            new LfcDiagnostic(LfcDiagnosticSeverity.Error, 3, "e1"),
            new LfcDiagnostic(LfcDiagnosticSeverity.Warning, 4, "w"),
            new LfcDiagnostic(LfcDiagnosticSeverity.Error, 5, "e2"),
        };

        Assert.Equal(new[] { "エラー", "警告", "エラー" }, ListItemBuilder.BuildDiagnosticItems(diagnostics).Select(d => d.SeverityText));
        Assert.Equal(2, ListItemBuilder.CountDiagnostics(diagnostics, LfcDiagnosticSeverity.Error));
        Assert.Equal(1, ListItemBuilder.CountDiagnostics(diagnostics, LfcDiagnosticSeverity.Warning));
        Assert.Empty(ListItemBuilder.BuildDiagnosticItems(Array.Empty<LfcDiagnostic>()));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ListItemBuilder.BuildSectionItems(null!));
        Assert.Throws<ArgumentNullException>(() => ListItemBuilder.BuildKeyItems(null!, "A"));
        Assert.Throws<ArgumentNullException>(() => ListItemBuilder.BuildKeyItems(new DocumentSession(), null!));
        Assert.Throws<ArgumentNullException>(() => ListItemBuilder.BuildDiagnosticItems(null!));
    }

    private static async Task<DocumentSession> OpenAsync(string content)
    {
        using var dir = new TemporaryDirectory();
        var session = new DocumentSession();
        await session.OpenFileAsync(dir.WriteText("a.lfc", content));
        return session;
    }
}
