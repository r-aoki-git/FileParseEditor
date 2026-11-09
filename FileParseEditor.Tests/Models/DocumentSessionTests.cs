using FileParseEditor.Core.Models;
using FileParseEditor.Tests.Helpers;
using FileParserLib;

namespace FileParseEditor.Tests.Models;

/// <summary>DocumentSession：ファイルを開く・新規作成のテスト（基本設計書 6-1、6-2、6-11）。</summary>
public class DocumentSessionTests
{
    private const string NormalContent = "Version = 1\n[Profile]\nName = \"田代\"\n";

    // 3行目（重複キー）と4行目（セクション記法の誤り）が Error、5行目が Warning
    private const string ErrorContent = "[Profile]\nName = \"田代\"\nName = \"青木\"\n[Broken\nMode = Auto\n";

    [Fact]
    public void Initial_IsEmptyUntitledDocument()
    {
        var session = new DocumentSession();

        Assert.Empty(session.Document.Sections);
        Assert.Null(session.FilePath);
        Assert.Empty(session.LoadDiagnostics);
        Assert.Equal("", session.CurrentText);
        Assert.False(session.IsModified);
    }

    [Fact]
    public void Open_LoadsDocumentAndKeepsFullPath()
    {
        using var dir = new TemporaryDirectory();
        var path = dir.WriteText("a.lfc", NormalContent);
        var session = new DocumentSession();

        session.Open(path);

        Assert.Equal(new[] { "", "Profile" }, session.Document.Sections);
        Assert.Equal(Path.GetFullPath(path), session.FilePath);
        Assert.Empty(session.LoadDiagnostics);
        Assert.Equal(session.Document.ToText(), session.CurrentText);
        Assert.False(session.IsModified);
    }

    [Fact]
    public void Open_RelativePath_IsKeptAsFullPath()
    {
        using var dir = new TemporaryDirectory();
        var path = dir.WriteText("a.lfc", NormalContent);
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, path);
        var session = new DocumentSession();

        session.Open(relative);

        Assert.Equal(Path.GetFullPath(path), session.FilePath);
        Assert.True(Path.IsPathFullyQualified(session.FilePath!));
    }

    [Fact]
    public void Open_FileWithErrors_DoesNotThrow_AndKeepsDiagnostics()
    {
        using var dir = new TemporaryDirectory();
        var path = dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();

        session.Open(path);

        Assert.Equal(
            new[] { (LfcDiagnosticSeverity.Error, 3), (LfcDiagnosticSeverity.Error, 4), (LfcDiagnosticSeverity.Warning, 5) },
            session.LoadDiagnostics.Select(d => (d.Severity, d.Line)));
        // Error の行は読み込まれない（重複キーは先勝ち、崩れたセクション行の後のキーは直前のセクションに入る）
        Assert.Equal(new LfcValue[] { "田代" }, session.Document.Get("Profile", "Name"));
        Assert.Equal(new[] { "Name", "Mode" }, session.Document.Keys("Profile"));
    }

    [Fact]
    public void Open_FileWithErrors_IsNotModified_EvenThoughTextDiffersFromFile()
    {
        // 開いた時点の ToText() を「保存済みの内容」とするため、エラーの行が除かれていても「変更あり」にはならない
        using var dir = new TemporaryDirectory();
        var path = dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();

        session.Open(path);

        Assert.NotEqual(ErrorContent, session.CurrentText);
        Assert.False(session.IsModified);
    }

    [Fact]
    public void Open_FileNotFound_Throws_AndKeepsCurrentDocument()
    {
        using var dir = new TemporaryDirectory();
        var path = dir.WriteText("a.lfc", NormalContent);
        var session = new DocumentSession();
        session.Open(path);
        var document = session.Document;

        Assert.Throws<FileNotFoundException>(() => session.Open(Path.Combine(dir.Path, "missing.lfc")));

        Assert.Same(document, session.Document);
        Assert.Equal(Path.GetFullPath(path), session.FilePath);
    }

    [Fact]
    public void Open_Directory_Throws_AndKeepsCurrentDocument()
    {
        using var dir = new TemporaryDirectory();
        var session = new DocumentSession();

        // フォルダを開こうとした場合の例外の種類は OS によって異なるが、いずれも状態を変えない
        Assert.ThrowsAny<Exception>(() => session.Open(dir.Path));

        Assert.Null(session.FilePath);
        Assert.Empty(session.Document.Sections);
    }

    [Fact]
    public void Open_Null_Throws()
    {
        Assert.Equal("path", Assert.Throws<ArgumentNullException>(() => new DocumentSession().Open(null!)).ParamName);
    }

    [Fact]
    public void Open_AnotherFile_ReplacesDocumentPathAndDiagnostics()
    {
        using var dir = new TemporaryDirectory();
        var errorPath = dir.WriteText("errors.lfc", ErrorContent);
        var normalPath = dir.WriteText("normal.lfc", NormalContent);
        var session = new DocumentSession();
        session.Open(errorPath);

        session.Open(normalPath);

        Assert.Equal(Path.GetFullPath(normalPath), session.FilePath);
        Assert.Empty(session.LoadDiagnostics);
        Assert.Equal(new[] { "", "Profile" }, session.Document.Sections);
    }

    [Fact]
    public void New_AfterOpen_ResetsToEmptyUntitledDocument()
    {
        using var dir = new TemporaryDirectory();
        var path = dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();
        session.Open(path);

        session.New();

        Assert.Empty(session.Document.Sections);
        Assert.Null(session.FilePath);
        Assert.Empty(session.LoadDiagnostics);
        Assert.Equal("", session.CurrentText);
        Assert.False(session.IsModified);
    }
}
