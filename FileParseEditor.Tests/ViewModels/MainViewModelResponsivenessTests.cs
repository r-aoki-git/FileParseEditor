using System.Collections.Specialized;
using FileParseEditor.Core.Mvvm;
using FileParseEditor.Core.ViewModels;
using FileParseEditor.Tests.Helpers;

namespace FileParseEditor.Tests.ViewModels;

/// <summary>
/// MainViewModel：画面が止まらないための仕組みのテスト。
/// ファイルの非同期な読み込みと読み込み中の状態（基本設計書 7-6）、一覧の変更通知を1回にまとめること（7-7）。
/// </summary>
public class MainViewModelResponsivenessTests : IDisposable
{
    private const string Content = "G = 1\n[Profile]\nName = \"田代\"\nAge = 30\n[Section]\nList = 10,20\n";

    private readonly TemporaryDirectory _dir = new();
    private readonly FakeFileDialogService _fileDialog = new();
    private readonly FakeMessageService _messages = new();
    private readonly MainViewModel _viewModel;

    public MainViewModelResponsivenessTests()
    {
        _viewModel = new MainViewModel(_fileDialog, _messages, new FakeInputDialogService());
    }

    public void Dispose() => _dir.Dispose();

    // ---- 読み込み中の状態（基本設計書 7-6） ----

    [Fact]
    public async Task WhileLoading_IsBusy_AllCommandsDisabled_AndStatusShowsPath()
    {
        var path = _dir.WriteText("a.lfc", Content);
        var observed = new List<(bool Busy, bool AnyCommandEnabled, string Status)>();
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsBusy))
            {
                observed.Add((_viewModel.IsBusy, AnyCommandEnabled(), _viewModel.StatusText));
            }
        };

        await _viewModel.OpenDroppedFilesAsync(new[] { path });

        Assert.Equal(2, observed.Count);
        Assert.True(observed[0].Busy);
        Assert.False(observed[0].AnyCommandEnabled);   // 読み込み中は、どの操作も実行できない
        Assert.False(observed[1].Busy);
        Assert.True(observed[1].AnyCommandEnabled);
        Assert.False(_viewModel.IsBusy);
        Assert.Equal("FileParseEditor - a.lfc", _viewModel.Title);
    }

    [Fact]
    public async Task WhileLoading_StatusShowsLoadingFile()
    {
        var path = _dir.WriteText("a.lfc", Content);
        var statusWhileBusy = "";
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.StatusText) && _viewModel.IsBusy)
            {
                statusWhileBusy = _viewModel.StatusText;
            }
        };

        await _viewModel.OpenDroppedFilesAsync(new[] { path });

        Assert.Equal($"読み込み中: {path}", statusWhileBusy);
        Assert.StartsWith(Path.GetFullPath(path), _viewModel.StatusText);
    }

    [Fact]
    public async Task WhileLoading_DroppedFileIsIgnored()
    {
        var first = _dir.WriteText("first.lfc", Content);
        var second = _dir.WriteText("second.lfc", "[Second]\n");
        Task? droppedWhileBusy = null;
        var completedImmediately = false;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsBusy) && _viewModel.IsBusy && droppedWhileBusy is null)
            {
                droppedWhileBusy = _viewModel.OpenDroppedFilesAsync(new[] { second });
                completedImmediately = droppedWhileBusy.IsCompleted;
            }
        };

        await _viewModel.OpenDroppedFilesAsync(new[] { first });
        await droppedWhileBusy!;

        Assert.True(completedImmediately);   // 読み込み中は、何もせずにすぐ戻る
        Assert.Equal("FileParseEditor - first.lfc", _viewModel.Title);
        Assert.DoesNotContain(_viewModel.Sections, s => s.Name == "Second");
        Assert.Empty(_messages.Errors);
    }

    [Fact]
    public async Task LoadingFailed_IsNotBusy_AndStatusIsRestored()
    {
        var before = _viewModel.StatusText;

        await _viewModel.OpenDroppedFilesAsync(new[] { Path.Combine(_dir.Path, "missing.lfc") });

        Assert.False(_viewModel.IsBusy);
        Assert.True(AnyCommandEnabled());
        Assert.Equal(before, _viewModel.StatusText);
        Assert.Single(_messages.Errors);
    }

    [Fact]
    public async Task OpenFileCommand_CannotBeExecutedTwiceAtTheSameTime()
    {
        _fileDialog.OpenFileDialogResult = _dir.WriteText("a.lfc", Content);
        var command = (AsyncRelayCommand)_viewModel.OpenFileCommand;
        var canExecuteWhileBusy = true;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsBusy) && _viewModel.IsBusy)
            {
                canExecuteWhileBusy = command.CanExecute(null);
            }
        };

        await command.ExecuteAsync();

        Assert.False(canExecuteWhileBusy);
        Assert.Equal(1, _fileDialog.OpenFileDialogCallCount);
    }

    // ---- 一覧の変更通知（基本設計書 7-7） ----

    [Fact]
    public async Task Open_NotifiesEachListOnlyOnce()
    {
        var sections = CountCollectionChanged(_viewModel.Sections);
        var keys = CountCollectionChanged(_viewModel.Keys);
        var diagnostics = CountCollectionChanged(_viewModel.Diagnostics);

        await _viewModel.OpenDroppedFilesAsync(new[] { _dir.WriteText("a.lfc", Content) });

        Assert.Equal(1, sections());
        Assert.Equal(1, keys());
        Assert.Equal(1, diagnostics());
        Assert.Equal(3, _viewModel.Sections.Count);
    }

    [Fact]
    public async Task ChangeSection_NotifiesKeyListOnlyOnce_EvenWithManyKeys()
    {
        var content = "[Many]\n" + string.Concat(Enumerable.Range(1, 500).Select(i => $"K{i} = {i}\n"));
        await _viewModel.OpenDroppedFilesAsync(new[] { _dir.WriteText("many.lfc", content) });
        var keys = CountCollectionChanged(_viewModel.Keys);

        _viewModel.SelectedSection = _viewModel.Sections.Single(s => s.Name == "Many");

        Assert.Equal(1, keys());
        Assert.Equal(500, _viewModel.Keys.Count);
    }

    [Fact]
    public void Lists_CannotBeModifiedFromOutside()
    {
        // 一覧の中身は ViewModel だけが入れ替える（画面やテストからは読み取り専用）
        Assert.True(((IList<SectionListItem>)_viewModel.Sections).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<KeyListItem>)_viewModel.Keys).Add(new KeyListItem("K", "1", "Int")));
    }

    private bool AnyCommandEnabled() =>
        new[]
        {
            _viewModel.CreateNewDocumentCommand, _viewModel.OpenFileCommand, _viewModel.AddSectionCommand, _viewModel.AddKeyCommand,
        }.Any(c => c.CanExecute(null));

    private static Func<int> CountCollectionChanged(INotifyCollectionChanged collection)
    {
        var count = 0;
        collection.CollectionChanged += (_, _) => count++;
        return () => count;
    }
}
