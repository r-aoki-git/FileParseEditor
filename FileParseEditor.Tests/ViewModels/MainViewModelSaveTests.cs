using FileParseEditor.Core.Mvvm;
using FileParseEditor.Core.Services;
using FileParseEditor.Core.ViewModels;
using FileParseEditor.Tests.Helpers;

namespace FileParseEditor.Tests.ViewModels;

/// <summary>
/// MainViewModel：上書き保存・名前を付けて保存（基本設計書 6-8）と、
/// 保存していない変更の確認（6-10、8-5）のテスト。
/// </summary>
public class MainViewModelSaveTests : IDisposable
{
    private const string Content = "[Profile]\nName = \"田代\"\nAge = 30\n";

    // 3行目（重複キー）が Error
    private const string ErrorContent = "[A]\nK = 1\nK = 2\n";

    private const string SaveChangesQuestionUntitled = "無題 への変更を保存しますか？";

    private readonly TemporaryDirectory _dir = new();
    private readonly FakeFileDialogService _fileDialog = new();
    private readonly FakeMessageService _messages = new();
    private readonly FakeInputDialogService _inputDialog = new();
    private readonly MainViewModel _viewModel;

    public MainViewModelSaveTests()
    {
        _viewModel = new MainViewModel(_fileDialog, _messages, _inputDialog);
    }

    public void Dispose() => _dir.Dispose();

    // ---- 上書き保存 ----

    [Fact]
    public async Task Save_OpenedFile_OverwritesWithoutDialog_AndClearsModified()
    {
        var path = await OpenAsync("a.lfc", Content);
        EditAge("31");
        Assert.Equal("FileParseEditor - a.lfc *", _viewModel.Title);

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Empty(_fileDialog.SaveFileDialogInitialPaths);
        Assert.Equal("[Profile]\nName = \"田代\"\nAge = 31\n", await File.ReadAllTextAsync(path));
        Assert.Equal("FileParseEditor - a.lfc", _viewModel.Title);
        Assert.Empty(_messages.Errors);
    }

    [Fact]
    public async Task Save_UnmodifiedFile_StillWrites()
    {
        // 「変更あり」でなくても上書き保存できる（基本設計書 3-3：常に実行できる）
        var path = await OpenAsync("a.lfc", "[Profile]\nName   =   \"田代\"\n");

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Equal("[Profile]\nName = \"田代\"\n", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Save_Untitled_ShowsDialogWithUntitledName()
    {
        AddKey("K", "1");
        var path = Path.Combine(_dir.Path, "new.lfc");
        _fileDialog.SaveFileDialogResult = path;

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Equal(new[] { "無題.lfc" }, _fileDialog.SaveFileDialogInitialPaths);
        Assert.Equal("K = 1\n", await File.ReadAllTextAsync(path));
        Assert.Equal("FileParseEditor - new.lfc", _viewModel.Title);
        Assert.StartsWith(Path.GetFullPath(path), _viewModel.StatusText);
    }

    [Fact]
    public async Task Save_Untitled_DialogCancelled_ChangesNothing()
    {
        AddKey("K", "1");

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Single(_fileDialog.SaveFileDialogInitialPaths);
        Assert.Equal("FileParseEditor - 無題 *", _viewModel.Title);
        Assert.Empty(Directory.GetFiles(_dir.Path));
    }

    // ---- 名前を付けて保存 ----

    [Fact]
    public async Task SaveAs_ShowsDialogWithCurrentPath_AndSwitchesToNewFile()
    {
        var original = await OpenAsync("a.lfc", Content);
        EditAge("31");
        var copy = Path.Combine(_dir.Path, "copy.lfc");
        _fileDialog.SaveFileDialogResult = copy;

        await ExecuteAsync(_viewModel.SaveFileAsCommand);

        Assert.Equal(new[] { Path.GetFullPath(original) }, _fileDialog.SaveFileDialogInitialPaths);
        Assert.Equal(Content, await File.ReadAllTextAsync(original));   // 元のファイルは変わらない
        Assert.Contains("Age = 31", await File.ReadAllTextAsync(copy));
        Assert.Equal("FileParseEditor - copy.lfc", _viewModel.Title);

        // 以降の上書き保存は、新しいファイルに保存する
        EditAge("32");
        await ExecuteAsync(_viewModel.SaveFileCommand);
        Assert.Contains("Age = 32", await File.ReadAllTextAsync(copy));
        Assert.Equal(Content, await File.ReadAllTextAsync(original));
    }

    [Fact]
    public async Task SaveAs_Cancelled_ChangesNothing()
    {
        var path = await OpenAsync("a.lfc", Content);
        EditAge("31");

        await ExecuteAsync(_viewModel.SaveFileAsCommand);

        Assert.Equal("FileParseEditor - a.lfc *", _viewModel.Title);
        Assert.Equal(Content, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task SaveAs_KeepsUndoHistory()
    {
        AddKey("K", "1");
        _fileDialog.SaveFileDialogResult = Path.Combine(_dir.Path, "a.lfc");

        await ExecuteAsync(_viewModel.SaveFileAsCommand);

        Assert.True(_viewModel.UndoEditCommand.CanExecute(null));
    }

    // ---- エラーを含むファイル（M03） ----

    [Fact]
    public async Task Save_ErrorFile_Yes_Overwrites_AndDoesNotAskAgain()
    {
        var path = await OpenAsync("errors.lfc", ErrorContent);
        _messages.ConfirmAnswers.Enqueue(true);

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Equal(
            new[] { "このファイルはエラーの行を読み込まずに開いています。上書きすると、元のファイルからエラーの行が失われます。保存しますか？" },
            _messages.Confirmations);
        Assert.Equal("[A]\nK = 1\n", await File.ReadAllTextAsync(path));

        await ExecuteAsync(_viewModel.SaveFileCommand);
        Assert.Single(_messages.Confirmations);   // 2回目は確認しない
    }

    [Fact]
    public async Task Save_ErrorFile_No_DoesNotWrite()
    {
        var path = await OpenAsync("errors.lfc", ErrorContent);
        _messages.ConfirmAnswers.Enqueue(false);

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Equal(ErrorContent, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task SaveAs_ErrorFile_ToOtherFile_DoesNotAsk()
    {
        var path = await OpenAsync("errors.lfc", ErrorContent);
        _fileDialog.SaveFileDialogResult = Path.Combine(_dir.Path, "fixed.lfc");

        await ExecuteAsync(_viewModel.SaveFileAsCommand);

        Assert.Empty(_messages.Confirmations);
        Assert.Equal(ErrorContent, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task SaveAs_ErrorFile_ToSameFile_Asks()
    {
        var path = await OpenAsync("errors.lfc", ErrorContent);
        _fileDialog.SaveFileDialogResult = path;
        _messages.ConfirmAnswers.Enqueue(false);

        await ExecuteAsync(_viewModel.SaveFileAsCommand);

        Assert.Single(_messages.Confirmations);
        Assert.Equal(ErrorContent, await File.ReadAllTextAsync(path));
    }

    // ---- 保存の失敗（M12） ----

    [Fact]
    public async Task SaveFailed_ShowsError_AndKeepsModified()
    {
        AddKey("K", "1");
        var path = Path.Combine(_dir.Path, "missing", "a.lfc");
        _fileDialog.SaveFileDialogResult = path;

        await ExecuteAsync(_viewModel.SaveFileAsCommand);

        var error = Assert.Single(_messages.Errors);
        Assert.StartsWith("ファイルを保存できませんでした。\n" + path + "\n", error);
        Assert.Equal("FileParseEditor - 無題 *", _viewModel.Title);
        Assert.False(_viewModel.IsBusy);
        Assert.StartsWith("無題", _viewModel.StatusText);
    }

    // ---- 保存中の状態（基本設計書 7-6） ----

    [Fact]
    public async Task WhileSaving_IsBusy_AllCommandsDisabled()
    {
        await OpenAsync("a.lfc", Content);
        var observed = new List<(bool Busy, bool SaveEnabled, bool EditEnabled, string Status)>();
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsBusy))
            {
                observed.Add((_viewModel.IsBusy, _viewModel.SaveFileCommand.CanExecute(null), _viewModel.AddSectionCommand.CanExecute(null), _viewModel.StatusText));
            }
        };

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Equal(2, observed.Count);
        Assert.True(observed[0].Busy);
        Assert.False(observed[0].SaveEnabled);
        Assert.False(observed[0].EditEnabled);
        Assert.False(observed[1].Busy);
        Assert.True(observed[1].EditEnabled);
    }

    [Fact]
    public async Task WhileSaving_StatusShowsPath()
    {
        var path = await OpenAsync("a.lfc", Content);
        var statusWhileBusy = "";
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.StatusText) && _viewModel.IsBusy)
            {
                statusWhileBusy = _viewModel.StatusText;
            }
        };

        await ExecuteAsync(_viewModel.SaveFileCommand);

        Assert.Equal($"保存中: {Path.GetFullPath(path)}", statusWhileBusy);
    }

    // ---- 保存していない変更の確認（M01） ----

    [Fact]
    public async Task Open_Unmodified_DoesNotAsk()
    {
        await OpenAsync("a.lfc", Content);

        await OpenAsync("b.lfc", Content);

        Assert.Empty(_messages.SaveChangesQuestions);
        Assert.Equal("FileParseEditor - b.lfc", _viewModel.Title);
    }

    [Fact]
    public async Task Open_Modified_Cancel_KeepsDocument_AndShowsNoDialog()
    {
        await OpenAsync("a.lfc", Content);
        EditAge("31");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Cancel);
        _fileDialog.OpenFileDialogResult = _dir.WriteText("b.lfc", Content);

        await ExecuteAsync(_viewModel.OpenFileCommand);

        Assert.Equal(new[] { "a.lfc への変更を保存しますか？" }, _messages.SaveChangesQuestions);
        Assert.Equal(0, _fileDialog.OpenFileDialogCallCount);   // キャンセルしたため、開くダイアログは表示しない
        Assert.Equal("FileParseEditor - a.lfc *", _viewModel.Title);
    }

    [Fact]
    public async Task Open_Modified_Discard_OpensWithoutSaving()
    {
        var first = await OpenAsync("a.lfc", Content);
        EditAge("31");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Discard);

        await OpenAsync("b.lfc", Content);

        Assert.Equal("FileParseEditor - b.lfc", _viewModel.Title);
        Assert.Equal(Content, await File.ReadAllTextAsync(first));
    }

    [Fact]
    public async Task Open_Modified_Save_SavesThenOpens()
    {
        var first = await OpenAsync("a.lfc", Content);
        EditAge("31");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Save);

        await OpenAsync("b.lfc", Content);

        Assert.Contains("Age = 31", await File.ReadAllTextAsync(first));
        Assert.Equal("FileParseEditor - b.lfc", _viewModel.Title);
    }

    [Fact]
    public async Task Open_ModifiedUntitled_Save_CancelInSaveDialog_AbortsOpen()
    {
        AddKey("K", "1");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Save);
        _fileDialog.OpenFileDialogResult = _dir.WriteText("b.lfc", Content);

        await ExecuteAsync(_viewModel.OpenFileCommand);   // 保存のダイアログはキャンセル（結果を設定していない）

        Assert.Equal(new[] { SaveChangesQuestionUntitled }, _messages.SaveChangesQuestions);
        Assert.Single(_fileDialog.SaveFileDialogInitialPaths);
        Assert.Equal(0, _fileDialog.OpenFileDialogCallCount);   // 開く操作も中止する
        Assert.Equal("FileParseEditor - 無題 *", _viewModel.Title);
    }

    [Fact]
    public async Task Open_Modified_SaveFails_AbortsOpen()
    {
        AddKey("K", "1");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Save);
        _fileDialog.SaveFileDialogResult = Path.Combine(_dir.Path, "missing", "a.lfc");

        await _viewModel.OpenDroppedFilesAsync(new[] { _dir.WriteText("b.lfc", Content) });

        Assert.Single(_messages.Errors);
        Assert.Equal("FileParseEditor - 無題 *", _viewModel.Title);
    }

    [Fact]
    public async Task Drop_Modified_AsksBeforeOpening()
    {
        AddKey("K", "1");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Cancel);

        await _viewModel.OpenDroppedFilesAsync(new[] { _dir.WriteText("b.lfc", Content) });

        Assert.Single(_messages.SaveChangesQuestions);
        Assert.Equal("FileParseEditor - 無題 *", _viewModel.Title);
    }

    [Fact]
    public async Task Drop_SeveralFiles_ShowsErrorWithoutAsking()
    {
        AddKey("K", "1");

        await _viewModel.OpenDroppedFilesAsync(new[] { _dir.WriteText("a.lfc", Content), _dir.WriteText("b.lfc", Content) });

        Assert.Empty(_messages.SaveChangesQuestions);   // 開けないことが先に分かるため、保存の確認はしない
        Assert.Single(_messages.Errors);
    }

    [Fact]
    public async Task New_Modified_Cancel_KeepsDocument()
    {
        AddKey("K", "1");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Cancel);

        await ExecuteAsync(_viewModel.CreateNewDocumentCommand);

        Assert.Equal(new[] { SaveChangesQuestionUntitled }, _messages.SaveChangesQuestions);
        Assert.Equal("K = 1\n", _viewModel.PreviewText);
    }

    [Fact]
    public async Task New_Modified_Discard_CreatesNewDocument()
    {
        AddKey("K", "1");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Discard);

        await ExecuteAsync(_viewModel.CreateNewDocumentCommand);

        Assert.Equal("", _viewModel.PreviewText);
        Assert.Equal("FileParseEditor - 無題", _viewModel.Title);
    }

    [Fact]
    public async Task UndoBackToSavedContent_DoesNotAsk()
    {
        await OpenAsync("a.lfc", Content);
        EditAge("31");
        _viewModel.UndoEditCommand.Execute(null);

        await ExecuteAsync(_viewModel.CreateNewDocumentCommand);

        // 開いた時点と同じ内容に戻っているため、「変更あり」ではない
        Assert.Empty(_messages.SaveChangesQuestions);
    }

    // ---- 閉じる（基本設計書 8-5） ----

    [Fact]
    public async Task Close_Unmodified_Closes()
    {
        await OpenAsync("a.lfc", Content);

        Assert.True(await _viewModel.ConfirmCloseWindowAsync());
        Assert.Empty(_messages.SaveChangesQuestions);
    }

    [Theory]
    [InlineData(SaveChangesChoice.Cancel, false)]
    [InlineData(SaveChangesChoice.Discard, true)]
    public async Task Close_Modified_FollowsChoice(SaveChangesChoice choice, bool expected)
    {
        var path = await OpenAsync("a.lfc", Content);
        EditAge("31");
        _messages.SaveChangesAnswers.Enqueue(choice);

        Assert.Equal(expected, await _viewModel.ConfirmCloseWindowAsync());
        Assert.Equal(Content, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Close_Modified_Save_SavesThenCloses()
    {
        var path = await OpenAsync("a.lfc", Content);
        EditAge("31");
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Save);

        Assert.True(await _viewModel.ConfirmCloseWindowAsync());
        Assert.Contains("Age = 31", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Close_ModifiedErrorFile_Save_No_DoesNotClose()
    {
        // 保存の確認で「はい」でも、エラーの行が失われる確認で「いいえ」なら保存せず、閉じない
        var path = await OpenAsync("errors.lfc", ErrorContent);
        _viewModel.SelectedSection = _viewModel.Sections.Single(s => s.Name == "A");
        _viewModel.SelectedKey = _viewModel.Keys.Single();
        _viewModel.EditingValueText = "3";
        _viewModel.ApplyEditingValueCommand.Execute(null);
        _messages.SaveChangesAnswers.Enqueue(SaveChangesChoice.Save);
        _messages.ConfirmAnswers.Enqueue(false);

        Assert.False(await _viewModel.ConfirmCloseWindowAsync());
        Assert.Equal(ErrorContent, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Close_WhileLoading_DoesNotClose()
    {
        var path = _dir.WriteText("a.lfc", Content);
        bool? closableWhileBusy = null;
        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsBusy) && _viewModel.IsBusy && closableWhileBusy is null)
            {
                closableWhileBusy = await _viewModel.ConfirmCloseWindowAsync();
            }
        };

        await _viewModel.OpenDroppedFilesAsync(new[] { path });

        Assert.False(closableWhileBusy);
    }

    private async Task<string> OpenAsync(string fileName, string content)
    {
        var path = _dir.WriteText(fileName, content);
        await _viewModel.OpenDroppedFilesAsync(new[] { path });
        return path;
    }

    private void EditAge(string valueText)
    {
        _viewModel.SelectedSection = _viewModel.Sections.Single(s => s.Name == "Profile");
        _viewModel.SelectedKey = _viewModel.Keys.Single(k => k.Key == "Age");
        _viewModel.EditingValueText = valueText;
        _viewModel.ApplyEditingValueCommand.Execute(null);
    }

    private void AddKey(string key, string valueText)
    {
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput(key, valueText));
        _viewModel.AddKeyCommand.Execute(null);
    }

    private static Task ExecuteAsync(System.Windows.Input.ICommand command) => ((AsyncRelayCommand)command).ExecuteAsync();
}
