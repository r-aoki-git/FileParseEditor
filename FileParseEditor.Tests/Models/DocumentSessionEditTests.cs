using FileParseEditor.Core.Models;
using FileParseEditor.Tests.Helpers;
using FileParserLib;

namespace FileParseEditor.Tests.Models;

/// <summary>DocumentSession：編集と元に戻すのテスト（基本設計書 6-3〜6-7、6-9、6-11）。</summary>
public class DocumentSessionEditTests : IDisposable
{
    private const string Content = "# 先頭\n[Profile] # P\n# 名前\nName = \"田代\" # N\n# 年齢\nAge = 30\nMemo =\n\n[Section]\nList = 10,20\n";

    private readonly TemporaryDirectory _dir = new();
    private readonly DocumentSession _session = new();
    private readonly string _openedText;

    public DocumentSessionEditTests()
    {
        _session.Open(_dir.WriteText("a.lfc", Content));
        _openedText = _session.CurrentText;
    }

    public void Dispose() => _dir.Dispose();

    // ---- 値の変更 ----

    [Fact]
    public void SetValue_ReplacesValues_KeepsPositionAndComments()
    {
        _session.SetValue("Profile", "Name", new LfcValue[] { "青木", 1 });

        Assert.Equal(new LfcValue[] { "青木", 1 }, _session.Document.Get("Profile", "Name"));
        Assert.Equal(new[] { "Name", "Age", "Memo" }, _session.Document.Keys("Profile"));
        Assert.Contains("# 名前\nName = \"青木\",1 # N\n", _session.CurrentText);
        Assert.True(_session.IsModified);
        Assert.True(_session.CanUndo);
    }

    [Fact]
    public void SetValue_DoesNotKeepReferenceToGivenList()
    {
        var values = new LfcValue[] { 1 };
        _session.SetValue("Profile", "Age", values);

        values[0] = 2;

        Assert.Equal(new LfcValue[] { 1 }, _session.Document.Get("Profile", "Age"));
    }

    [Fact]
    public void SetValue_MissingKey_Throws_AndChangesNothing()
    {
        Assert.Throws<KeyNotFoundException>(() => _session.SetValue("Profile", "Missing", new LfcValue[] { 1 }));
        Assert.Throws<KeyNotFoundException>(() => _session.SetValue("Missing", "Name", new LfcValue[] { 1 }));

        // ライブラリの Set のようにキーを追加してしまわない
        AssertUnchanged();
    }

    [Fact]
    public void SetValue_NoValues_Throws_AndRecordsNoHistory()
    {
        Assert.Throws<ArgumentException>(() => _session.SetValue("Profile", "Age", Array.Empty<LfcValue>()));

        AssertUnchanged();
    }

    [Fact]
    public void SetValue_NaN_Throws_AndRecordsNoHistory()
    {
        Assert.Throws<ArgumentException>(() => _session.SetValue("Profile", "Age", new LfcValue[] { double.NaN }));

        AssertUnchanged();
    }

    // ---- キーの追加・削除 ----

    [Fact]
    public void AddKey_AppendsToEndOfSection()
    {
        _session.AddKey("Profile", "Email", new LfcValue[] { "a@example.com" });

        Assert.Equal(new[] { "Name", "Age", "Memo", "Email" }, _session.Document.Keys("Profile"));
        Assert.True(_session.IsModified);
        Assert.True(_session.CanUndo);
    }

    [Fact]
    public void AddKey_ToMissingGlobalSection_CreatesItAtFront()
    {
        _session.AddKey(LfcDocument.GlobalSectionName, "Version", new LfcValue[] { 1 });

        Assert.Equal(new[] { "", "Profile", "Section" }, _session.Document.Sections);
        Assert.StartsWith("Version = 1\n", _session.CurrentText);
    }

    [Fact]
    public void AddKey_Existing_Throws_AndDoesNotOverwrite()
    {
        Assert.Throws<InvalidOperationException>(() => _session.AddKey("Profile", "Name", new LfcValue[] { "青木" }));

        Assert.Equal(new LfcValue[] { "田代" }, _session.Document.Get("Profile", "Name"));
        AssertUnchanged();
    }

    [Theory]
    [InlineData("a=b")]
    [InlineData("#a")]
    [InlineData("[a")]
    [InlineData(" a")]
    [InlineData("")]
    public void AddKey_InvalidName_Throws_AndRecordsNoHistory(string key)
    {
        Assert.Throws<ArgumentException>(() => _session.AddKey("Profile", key, new LfcValue[] { 1 }));

        AssertUnchanged();
    }

    [Fact]
    public void RemoveKey_RemovesKeyWithItsComment()
    {
        Assert.True(_session.RemoveKey("Profile", "Name"));

        Assert.Equal(new[] { "Age", "Memo" }, _session.Document.Keys("Profile"));
        Assert.DoesNotContain("# 名前", _session.CurrentText);
        Assert.True(_session.CanUndo);
    }

    [Fact]
    public void RemoveKey_Missing_ReturnsFalse_AndRecordsNoHistory()
    {
        Assert.False(_session.RemoveKey("Profile", "Missing"));
        Assert.False(_session.RemoveKey("Missing", "Name"));

        AssertUnchanged();
    }

    // ---- セクションの追加・削除 ----

    [Fact]
    public void AddSection_AppendsToEnd()
    {
        Assert.True(_session.AddSection("New"));

        Assert.Equal(new[] { "Profile", "Section", "New" }, _session.Document.Sections);
        Assert.Empty(_session.Document.Keys("New"));
        Assert.True(_session.CanUndo);
    }

    [Fact]
    public void AddSection_Existing_ReturnsFalse_AndRecordsNoHistory()
    {
        Assert.False(_session.AddSection("Profile"));

        AssertUnchanged();
    }

    [Theory]
    [InlineData("[a")]
    [InlineData("a]")]
    [InlineData("a ")]
    public void AddSection_InvalidName_Throws_AndRecordsNoHistory(string name)
    {
        Assert.Throws<ArgumentException>(() => _session.AddSection(name));

        AssertUnchanged();
    }

    [Fact]
    public void RemoveSection_RemovesAllKeys()
    {
        Assert.True(_session.RemoveSection("Profile"));

        Assert.Equal(new[] { "Section" }, _session.Document.Sections);
        Assert.False(_session.KeyExists("Profile", "Name"));
        Assert.True(_session.CanUndo);
    }

    [Fact]
    public void RemoveSection_Missing_ReturnsFalse_AndRecordsNoHistory()
    {
        Assert.False(_session.RemoveSection("Missing"));
        Assert.False(_session.RemoveSection(LfcDocument.GlobalSectionName));   // このファイルにはグローバルセクションがない

        AssertUnchanged();
    }

    [Fact]
    public void Exists_ChecksDocument()
    {
        Assert.True(_session.SectionExists("Profile"));
        Assert.False(_session.SectionExists("profile"));                      // 大文字・小文字を区別する
        Assert.False(_session.SectionExists(LfcDocument.GlobalSectionName));
        Assert.True(_session.KeyExists("Profile", "Name"));
        Assert.False(_session.KeyExists("Profile", "name"));
        Assert.False(_session.KeyExists("Missing", "Name"));
    }

    // ---- 元に戻す ----

    [Fact]
    public void Undo_RemovedKey_ComesBackToOriginalPositionWithComment()
    {
        // ライブラリの Set で戻すと末尾に追加されコメントも失われるが、元に戻すでは元どおりになる
        _session.RemoveKey("Profile", "Name");

        _session.Undo();

        Assert.Equal(new[] { "Name", "Age", "Memo" }, _session.Document.Keys("Profile"));
        Assert.Equal(_openedText, _session.CurrentText);
    }

    [Fact]
    public void Undo_RemovedSection_ComesBackWithAllKeys()
    {
        _session.RemoveSection("Profile");

        _session.Undo();

        Assert.Equal(new[] { "Profile", "Section" }, _session.Document.Sections);
        Assert.Equal(_openedText, _session.CurrentText);
    }

    [Fact]
    public void Undo_GoesBackOneStepAtATime()
    {
        _session.SetValue("Profile", "Age", new LfcValue[] { 31 });
        _session.AddKey("Profile", "Email", new LfcValue[] { "a" });
        _session.AddSection("New");

        _session.Undo();
        Assert.Equal(new[] { "Profile", "Section" }, _session.Document.Sections);
        Assert.True(_session.KeyExists("Profile", "Email"));

        _session.Undo();
        Assert.False(_session.KeyExists("Profile", "Email"));
        Assert.Equal(new LfcValue[] { 31 }, _session.Document.Get("Profile", "Age"));

        _session.Undo();
        Assert.Equal(new LfcValue[] { 30 }, _session.Document.Get("Profile", "Age"));
        Assert.False(_session.CanUndo);
    }

    [Fact]
    public void Undo_BackToOpenedContent_IsNotModified()
    {
        _session.SetValue("Profile", "Age", new LfcValue[] { 31 });
        Assert.True(_session.IsModified);

        _session.Undo();

        Assert.False(_session.IsModified);
    }

    [Fact]
    public void EditBackToOpenedContent_IsNotModified()
    {
        // 元に戻すを使わずに、同じ内容に編集し直した場合も「変更あり」を解除する（内容で比較するため）
        _session.SetValue("Profile", "Age", new LfcValue[] { 31 });
        _session.SetValue("Profile", "Age", new LfcValue[] { 30 });

        Assert.False(_session.IsModified);
        Assert.True(_session.CanUndo);
    }

    [Fact]
    public void Undo_WithoutHistory_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _session.Undo());

        AssertUnchanged();
    }

    [Fact]
    public void Undo_KeepsLoadDiagnostics()
    {
        // 作り直した文書には Error の行が含まれないため診断情報は0件になるが、表示するのは開いた時点のもの
        var session = new DocumentSession();
        session.Open(_dir.WriteText("errors.lfc", "[A]\nK = 1\nK = 2\n"));
        var diagnostics = session.LoadDiagnostics;
        session.SetValue("A", "K", new LfcValue[] { 3 });

        session.Undo();

        Assert.Same(diagnostics, session.LoadDiagnostics);
        Assert.Single(session.LoadDiagnostics);
    }

    [Fact]
    public void Open_ClearsHistory()
    {
        _session.SetValue("Profile", "Age", new LfcValue[] { 31 });

        _session.Open(_dir.WriteText("b.lfc", "K = 1\n"));

        Assert.False(_session.CanUndo);
    }

    [Fact]
    public void New_ClearsHistory()
    {
        _session.SetValue("Profile", "Age", new LfcValue[] { 31 });

        _session.New();

        Assert.False(_session.CanUndo);
        Assert.False(_session.IsModified);
    }

    [Fact]
    public void FailedOpen_KeepsHistory()
    {
        _session.SetValue("Profile", "Age", new LfcValue[] { 31 });

        Assert.Throws<FileNotFoundException>(() => _session.Open(Path.Combine(_dir.Path, "missing.lfc")));

        Assert.True(_session.CanUndo);
        Assert.True(_session.IsModified);
    }

    private void AssertUnchanged()
    {
        Assert.Equal(_openedText, _session.CurrentText);
        Assert.Equal(_openedText, _session.Document.ToText());
        Assert.False(_session.IsModified);
        Assert.False(_session.CanUndo);
    }
}
