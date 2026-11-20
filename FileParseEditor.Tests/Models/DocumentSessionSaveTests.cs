using FileParseEditor.Core.Models;
using FileParseEditor.Tests.Helpers;
using FileParserLib;

namespace FileParseEditor.Tests.Models;

/// <summary>DocumentSession：保存と、エラーの行が失われる確認の判定のテスト（基本設計書 6-8、6-11）。</summary>
public class DocumentSessionSaveTests : IDisposable
{
    private const string Content = "# コメント\n[Profile]\nName = \"田代\" # 行末\nAge = 30\n";

    // 3行目（重複キー）が Error
    private const string ErrorContent = "[A]\nK = 1\nK = 2\n";

    private readonly TemporaryDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    // ---- 保存 ----

    [Fact]
    public async Task Save_WritesToTextAsUtf8WithoutBom_WithLf()
    {
        var session = new DocumentSession();
        session.AddKey("S", "名前", new LfcValue[] { "田代" });
        var path = Path.Combine(_dir.Path, "new.lfc");

        await session.SaveFileAsync(path);

        var bytes = await File.ReadAllBytesAsync(path);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.DoesNotContain((byte)'\r', bytes);
        Assert.Equal(session.CurrentText, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Save_SetsFilePath_AndClearsModified()
    {
        var session = new DocumentSession();
        session.AddKey("S", "K", new LfcValue[] { 1 });
        Assert.True(session.IsModified);
        var path = Path.Combine(_dir.Path, "new.lfc");

        await session.SaveFileAsync(path);

        Assert.Equal(Path.GetFullPath(path), session.FilePath);
        Assert.False(session.IsModified);
    }

    [Fact]
    public async Task Save_RelativePath_IsKeptAsFullPath()
    {
        var session = new DocumentSession();
        var path = Path.Combine(_dir.Path, "new.lfc");

        await session.SaveFileAsync(Path.GetRelativePath(Environment.CurrentDirectory, path));

        Assert.Equal(Path.GetFullPath(path), session.FilePath);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task Save_OpenedFile_OverwritesWithCommentsKept()
    {
        var path = _dir.WriteText("a.lfc", Content);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);
        session.SetValue("Profile", "Age", new LfcValue[] { 31 });

        await session.SaveFileAsync(path);

        Assert.Equal("# コメント\n[Profile]\nName = \"田代\" # 行末\nAge = 31\n", await File.ReadAllTextAsync(path));
        Assert.False(session.IsModified);
    }

    [Fact]
    public async Task Save_KeepsUndoHistoryAndDiagnostics()
    {
        var path = _dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);
        var diagnostics = session.LoadDiagnostics;
        session.SetValue("A", "K", new LfcValue[] { 3 });

        await session.SaveFileAsync(Path.Combine(_dir.Path, "saved.lfc"));

        Assert.True(session.CanUndo);                        // 保存しても元に戻す履歴は消えない（要件定義書 4-4）
        Assert.Same(diagnostics, session.LoadDiagnostics);   // 診断情報は開いた時点のまま
    }

    [Fact]
    public async Task Save_ThenUndo_IsModifiedAgain()
    {
        var session = new DocumentSession();
        session.AddKey("S", "K", new LfcValue[] { 1 });
        await session.SaveFileAsync(Path.Combine(_dir.Path, "a.lfc"));

        session.UndoLastEdit();

        // 保存した内容と異なるため「変更あり」になる（保存した内容と比べるため）
        Assert.True(session.IsModified);
    }

    [Fact]
    public async Task Save_ToMissingFolder_Throws_AndKeepsState()
    {
        var session = new DocumentSession();
        session.AddKey("S", "K", new LfcValue[] { 1 });

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => session.SaveFileAsync(Path.Combine(_dir.Path, "missing", "a.lfc")));

        Assert.Null(session.FilePath);
        Assert.True(session.IsModified);
    }

    [Fact]
    public async Task Save_ToFolderPath_Throws_AndKeepsState()
    {
        var path = _dir.WriteText("a.lfc", Content);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);
        session.SetValue("Profile", "Age", new LfcValue[] { 31 });

        // フォルダを保存先にした場合の例外の種類は OS によって異なるが、いずれも状態を変えない
        await Assert.ThrowsAnyAsync<Exception>(() => session.SaveFileAsync(_dir.Path));

        Assert.Equal(Path.GetFullPath(path), session.FilePath);
        Assert.True(session.IsModified);
        Assert.Equal(Content, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Save_Null_Throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => new DocumentSession().SaveFileAsync(null!));
        Assert.Equal("path", exception.ParamName);
    }

    // ---- エラーの行が失われる確認（M03） ----

    [Fact]
    public async Task ErrorFile_SamePath_NeedsConfirmation()
    {
        var path = _dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);

        Assert.True(session.NeedsErrorLossConfirmation(path));
        Assert.True(session.NeedsErrorLossConfirmation(Path.GetRelativePath(Environment.CurrentDirectory, path)));   // 書き方が違っても同じファイル
    }

    [Fact]
    public async Task ErrorFile_OtherPath_DoesNotNeedConfirmation()
    {
        var session = new DocumentSession();
        await session.OpenFileAsync(_dir.WriteText("errors.lfc", ErrorContent));

        // 別のファイルに保存しても、元のファイルのエラーの行は失われない
        Assert.False(session.NeedsErrorLossConfirmation(Path.Combine(_dir.Path, "other.lfc")));
    }

    [Fact]
    public async Task ErrorFile_AfterOverwrite_DoesNotNeedConfirmationAgain()
    {
        var path = _dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);

        await session.SaveFileAsync(path);

        Assert.False(session.NeedsErrorLossConfirmation(path));
        Assert.Equal("[A]\nK = 1\n", await File.ReadAllTextAsync(path));   // エラーの行（K = 2）は失われた
    }

    [Fact]
    public async Task ErrorFile_SavedElsewhere_StillNeedsConfirmationForOriginal()
    {
        var path = _dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);

        await session.SaveFileAsync(Path.Combine(_dir.Path, "other.lfc"));

        Assert.True(session.NeedsErrorLossConfirmation(path));
        Assert.Equal(ErrorContent, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task ErrorFile_FailedOverwrite_StillNeedsConfirmation()
    {
        var path = _dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            if (await Record.ExceptionAsync(() => session.SaveFileAsync(path)) is null)
            {
                return;   // 管理者の権限で実行している場合など、読み取り専用でも書き込めた環境では確認できない
            }

            Assert.True(session.NeedsErrorLossConfirmation(path));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task FileWithoutErrors_DoesNotNeedConfirmation()
    {
        var path = _dir.WriteText("a.lfc", Content + "Mode = Auto\n");   // Warning だけのファイル
        var session = new DocumentSession();
        await session.OpenFileAsync(path);

        Assert.False(session.NeedsErrorLossConfirmation(path));
    }

    [Fact]
    public async Task NewOrAnotherFile_ClearsConfirmation()
    {
        var path = _dir.WriteText("errors.lfc", ErrorContent);
        var session = new DocumentSession();
        await session.OpenFileAsync(path);

        session.CreateNewDocument();
        Assert.False(session.NeedsErrorLossConfirmation(path));

        await session.OpenFileAsync(path);
        await session.OpenFileAsync(_dir.WriteText("a.lfc", Content));
        Assert.False(session.NeedsErrorLossConfirmation(path));
    }

    [Fact]
    public void NeedsErrorLossConfirmation_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentSession().NeedsErrorLossConfirmation(null!));
    }
}
