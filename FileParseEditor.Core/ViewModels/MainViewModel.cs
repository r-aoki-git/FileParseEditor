using System.Collections.ObjectModel;
using System.Security;
using System.Windows.Input;
using FileParseEditor.Core.Models;
using FileParseEditor.Core.Mvvm;
using FileParseEditor.Core.Services;
using FileParserLib;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// メイン画面の表示内容とコマンド（基本設計書 3章、7-5）。
/// 文書の操作は <see cref="DocumentSession"/> に、ダイアログを伴う編集の流れは <see cref="EditDialogFlow"/> に任せ、
/// このクラスは表示内容の更新と、コマンドの実行できる条件を担う。
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    internal const string ApplicationName = "FileParseEditor";
    internal const string UntitledName = "無題";

    private readonly DocumentSession _session = new();
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageService _messageService;
    private readonly ValueInputChecker _valueInputChecker;
    private readonly EditDialogFlow _editDialogFlow;

    // 一覧は作り直すたびに中身をまとめて入れ替え、画面への通知を1回にする（基本設計書 7-7）
    private readonly BulkObservableCollection<SectionListItem> _sections = new();
    private readonly BulkObservableCollection<KeyListItem> _keys = new();
    private readonly BulkObservableCollection<DiagnosticListItem> _diagnostics = new();

    // 実行できる条件が選択や文書の状態で変わるコマンド。状態が変わるたびに画面へ通知する
    private readonly CommandBase[] _commands;

    private SectionListItem? _selectedSection;
    private KeyListItem? _selectedKey;
    private string _editingValueText = "";
    private string _title = "";
    private string _statusText = "";
    private string _previewText = "";
    private bool _isBusy;

    public MainViewModel(IFileDialogService fileDialogService, IMessageService messageService, IInputDialogService inputDialogService)
    {
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(messageService);
        ArgumentNullException.ThrowIfNull(inputDialogService);
        _fileDialogService = fileDialogService;
        _messageService = messageService;
        _valueInputChecker = new ValueInputChecker(messageService);
        _editDialogFlow = new EditDialogFlow(_session, messageService, inputDialogService, _valueInputChecker);

        Sections = new ReadOnlyObservableCollection<SectionListItem>(_sections);
        Keys = new ReadOnlyObservableCollection<KeyListItem>(_keys);
        Diagnostics = new ReadOnlyObservableCollection<DiagnosticListItem>(_diagnostics);

        // 読み込み中（IsBusy）は、どの操作も実行できないようにする
        var createNewDocument = new RelayCommand(CreateNewDocument, () => !IsBusy);
        var openFile = new AsyncRelayCommand(OpenFileWithDialogAsync, () => !IsBusy);
        var undoEdit = new RelayCommand(UndoLastEdit, () => !IsBusy && _session.CanUndo);
        var addSection = new RelayCommand(AddSectionWithDialog, () => !IsBusy);
        var removeSelectedSection = new RelayCommand(RemoveSelectedSection, () => !IsBusy && SelectedSection is not null && _session.SectionExists(SelectedSection.Name));
        var addKey = new RelayCommand(AddKeyWithDialog, () => !IsBusy && SelectedSection is not null);
        var removeSelectedKey = new RelayCommand(RemoveSelectedKey, () => !IsBusy && SelectedKey is not null);
        var applyEditingValue = new RelayCommand(ApplyEditingValue, () => !IsBusy && SelectedKey is not null);
        CreateNewDocumentCommand = createNewDocument;
        OpenFileCommand = openFile;
        UndoEditCommand = undoEdit;
        AddSectionCommand = addSection;
        RemoveSelectedSectionCommand = removeSelectedSection;
        AddKeyCommand = addKey;
        RemoveSelectedKeyCommand = removeSelectedKey;
        ApplyEditingValueCommand = applyEditingValue;
        _commands = new CommandBase[]
        {
            createNewDocument, openFile, undoEdit, addSection, removeSelectedSection, addKey, removeSelectedKey, applyEditingValue,
        };

        RefreshAllViews();
    }

    /// <summary>タイトルバーの表示（「FileParseEditor - ファイル名」。変更ありの場合は末尾に「 *」）。</summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    /// <summary>ステータスバーの表示（ファイルのパス、セクション・警告・エラーの件数。読み込み中はその旨）。</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>保存イメージ（文書の ToText() の結果）。</summary>
    public string PreviewText
    {
        get => _previewText;
        private set => SetProperty(ref _previewText, value);
    }

    /// <summary>ファイルを読み込み中か。読み込み中はどの操作も実行できない（基本設計書 7-6）。</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCanExecuteChangedForAll();
            }
        }
    }

    /// <summary>セクション一覧。先頭は常にグローバルセクション（基本設計書 3-4-1）。</summary>
    public ReadOnlyObservableCollection<SectionListItem> Sections { get; }

    /// <summary>選択中のセクション。変えるとキー一覧が更新される。</summary>
    public SectionListItem? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                RefreshKeyList(null);
            }
        }
    }

    /// <summary>選択中のセクションのキー一覧。</summary>
    public ReadOnlyObservableCollection<KeyListItem> Keys { get; }

    /// <summary>選択中のキー。変えると値の編集欄に、そのキーの値の表記が入る（入力中の内容は破棄する。基本設計書 3-4-3）。</summary>
    public KeyListItem? SelectedKey
    {
        get => _selectedKey;
        set
        {
            if (!EqualityComparer<KeyListItem?>.Default.Equals(_selectedKey, value))
            {
                SelectKey(value);
            }
        }
    }

    /// <summary>キーを選択しているか（値の編集欄を入力できるか）。</summary>
    public bool IsKeySelected => SelectedKey is not null;

    /// <summary>値の編集欄の入力。</summary>
    public string EditingValueText
    {
        get => _editingValueText;
        set => SetProperty(ref _editingValueText, value ?? "");
    }

    /// <summary>ファイルを開いた時点の診断情報の一覧。</summary>
    public ReadOnlyObservableCollection<DiagnosticListItem> Diagnostics { get; }

    /// <summary>新規作成（基本設計書 6-2）。</summary>
    public ICommand CreateNewDocumentCommand { get; }

    /// <summary>ファイルを開くダイアログで選んだファイルを、非同期に開く（基本設計書 6-1）。</summary>
    public ICommand OpenFileCommand { get; }

    /// <summary>直前の編集を元に戻す（基本設計書 6-9）。元に戻せる履歴がある場合に実行できる。</summary>
    public ICommand UndoEditCommand { get; }

    /// <summary>セクションの追加（基本設計書 6-6）。</summary>
    public ICommand AddSectionCommand { get; }

    /// <summary>選択中のセクションの削除（基本設計書 6-7）。セクションが文書に存在する場合に実行できる。</summary>
    public ICommand RemoveSelectedSectionCommand { get; }

    /// <summary>選択中のセクションへのキーの追加（基本設計書 6-4）。セクションを選択している場合に実行できる。</summary>
    public ICommand AddKeyCommand { get; }

    /// <summary>選択中のキーの削除（基本設計書 6-5）。キーを選択している場合に実行できる。</summary>
    public ICommand RemoveSelectedKeyCommand { get; }

    /// <summary>値の編集欄の入力で、選択中のキーの値を変更する（基本設計書 6-3）。キーを選択している場合に実行できる。</summary>
    public ICommand ApplyEditingValueCommand { get; }

    /// <summary>
    /// ドラッグ＆ドロップされたファイルを、非同期に開く（基本設計書 6-1）。
    /// ファイル1つの場合だけ開き、複数のファイル・フォルダの場合はメッセージを表示する。読み込み中は何もしない。
    /// </summary>
    public async Task OpenDroppedFilesAsync(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (IsBusy)
        {
            return;
        }
        if (paths.Count != 1 || Directory.Exists(paths[0]))
        {
            _messageService.ShowError(MessageTexts.DropOneFileOnly);
            return;
        }
        await OpenFileAsync(paths[0]);
    }

    /// <summary>
    /// 想定外の例外を利用者に知らせる（基本設計書 9章）。アプリ全体の例外ハンドラーから呼び出す。
    /// </summary>
    public void ReportUnexpectedError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _messageService.ShowError(MessageTexts.UnexpectedError(MessageTexts.ExceptionDetail(exception)));
    }

    // ---- ファイル ----

    private void CreateNewDocument()
    {
        _session.CreateNewDocument();
        RefreshAllViews();
    }

    private async Task OpenFileWithDialogAsync()
    {
        var path = _fileDialogService.ShowOpenFileDialog();
        if (path is null)
        {
            return;
        }
        await OpenFileAsync(path);
    }

    private async Task OpenFileAsync(string path)
    {
        IsBusy = true;
        StatusText = $"読み込み中: {path}";
        try
        {
            await _session.OpenFileAsync(path);
        }
        catch (Exception ex) when (IsFileAccessError(ex))
        {
            // 読み込みに失敗した場合は、現在の文書をそのまま残す
            _messageService.ShowError(MessageTexts.OpenFailed(path, MessageTexts.ExceptionDetail(ex)));
            return;
        }
        finally
        {
            IsBusy = false;
            RefreshDocumentState();
        }

        RefreshAllViews();

        var errorCount = CountDiagnostics(LfcDiagnosticSeverity.Error);
        if (errorCount > 0)
        {
            _messageService.ShowInformation(MessageTexts.FileHasErrors(errorCount));
        }
    }

    /// <summary>ファイルの読み書きで発生する、利用者に知らせて操作を続けられる例外か。</summary>
    private static bool IsFileAccessError(Exception exception) =>
        exception is IOException                     // ファイル・フォルダがない、使用中など（FileNotFoundException 等を含む）
            or UnauthorizedAccessException           // アクセス権がない
            or ArgumentException                     // パスに使えない文字を含む、空のパス
            or NotSupportedException                 // パスの形式が正しくない
            or SecurityException;

    // ---- 編集 ----

    private void ApplyEditingValue()
    {
        var section = SelectedSection;
        var key = SelectedKey;
        if (section is null || key is null)
        {
            return;
        }

        var result = LfcValueNotation.ParseNotation(EditingValueText);
        if (!_valueInputChecker.ConfirmValueInput(result))
        {
            return;   // 入力は値の編集欄に残す
        }

        var current = _session.Document.Get(section.Name, key.Key);
        if (current.SequenceEqual(result.Values))
        {
            // 値が変わらない場合は変更として扱わない（表記だけを正規化して表示する）
            EditingValueText = LfcValueNotation.ToNotation(result.Values);
            return;
        }

        _session.SetValue(section.Name, key.Key, result.Values);
        RefreshKeyList(key.Key);
        RefreshDocumentState();
    }

    private void AddKeyWithDialog()
    {
        var section = SelectedSection;
        if (section is null)
        {
            return;
        }

        var addedKey = _editDialogFlow.AddKeyWithDialog(section);
        if (addedKey is null)
        {
            return;
        }
        RefreshKeyList(addedKey);
        RefreshDocumentState();
    }

    private void RemoveSelectedKey()
    {
        var section = SelectedSection;
        var key = SelectedKey;
        if (section is null || key is null)
        {
            return;
        }

        // 確認は行わない（元に戻すで戻せるため。基本設計書 6-5）
        var index = Keys.IndexOf(key);
        _session.RemoveKey(section.Name, key.Key);
        RefreshKeyList(null);

        // 削除したキーの次のキー（末尾だった場合は前のキー）を選択する
        if (Keys.Count > 0)
        {
            SelectKey(Keys[Math.Min(index, Keys.Count - 1)]);
        }
        RefreshDocumentState();
    }

    private void AddSectionWithDialog()
    {
        var sectionName = _editDialogFlow.AddSectionWithDialog();
        if (sectionName is null)
        {
            return;
        }
        RefreshAllViews(sectionName, null);
    }

    private void RemoveSelectedSection()
    {
        var section = SelectedSection;
        if (section is null || !_editDialogFlow.RemoveSectionAfterConfirmation(section))
        {
            return;
        }

        // グローバルセクションは削除しても一覧に残るため、選択したままにする
        RefreshAllViews(section.Name, null);
    }

    private void UndoLastEdit()
    {
        if (!_session.CanUndo)
        {
            return;
        }

        // 元に戻した後も、同じセクション・キーがあれば選択し直す（基本設計書 6-9）
        var sectionName = SelectedSection?.Name;
        var keyName = SelectedKey?.Key;
        _session.UndoLastEdit();
        RefreshAllViews(sectionName, keyName);
    }

    // ---- 表示の更新 ----

    /// <summary>
    /// 全ての表示を更新する。指定した名前のセクション・キーがあれば選択し、なければ先頭（グローバルセクション）を選択する。
    /// </summary>
    private void RefreshAllViews(string? sectionName = null, string? keyName = null)
    {
        _sections.ReplaceAll(ListItemBuilder.BuildSectionItems(_session));

        // 同じ内容の行（record は値で比較される）を選び直した場合も、キー一覧は必ず作り直す
        _selectedSection = (sectionName is null ? null : FindSection(sectionName)) ?? _sections[0];
        OnPropertyChanged(nameof(SelectedSection));
        RefreshKeyList(keyName);

        _diagnostics.ReplaceAll(ListItemBuilder.BuildDiagnosticItems(_session.LoadDiagnostics));

        RefreshDocumentState();
    }

    /// <summary>選択中のセクションのキー一覧を作り直す。指定した名前のキーがあれば選択する。</summary>
    private void RefreshKeyList(string? keyName)
    {
        // セクションを選択していない場合は0件とする
        var items = SelectedSection is null
            ? Array.Empty<KeyListItem>()
            : ListItemBuilder.BuildKeyItems(_session, SelectedSection.Name);
        _keys.ReplaceAll(items);

        KeyListItem? selected = null;
        foreach (var item in items)
        {
            if (item.Key == keyName)
            {
                selected = item;
                break;
            }
        }
        SelectKey(selected);
    }

    /// <summary>キーを選択し、値の編集欄をそのキーの値の表記にする。同じ値の行を選び直した場合も編集欄を更新する。</summary>
    private void SelectKey(KeyListItem? item)
    {
        _selectedKey = item;
        OnPropertyChanged(nameof(SelectedKey));
        OnPropertyChanged(nameof(IsKeySelected));
        EditingValueText = item?.ValueText ?? "";
        RaiseCanExecuteChangedForAll();
    }

    /// <summary>タイトル・ステータスバー・保存イメージと、コマンドの実行できる条件を更新する。</summary>
    private void RefreshDocumentState()
    {
        var fileName = _session.FilePath is null ? UntitledName : Path.GetFileName(_session.FilePath);
        Title = $"{ApplicationName} - {fileName}{(_session.IsModified ? " *" : "")}";

        StatusText = string.Join("    ",
            _session.FilePath ?? UntitledName,
            $"セクション {_session.Document.Sections.Count}件",
            $"警告 {CountDiagnostics(LfcDiagnosticSeverity.Warning)}件",
            $"エラー {CountDiagnostics(LfcDiagnosticSeverity.Error)}件");

        PreviewText = _session.CurrentText;
        RaiseCanExecuteChangedForAll();
    }

    private void RaiseCanExecuteChangedForAll()
    {
        foreach (var command in _commands)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private SectionListItem? FindSection(string name)
    {
        foreach (var section in _sections)
        {
            if (section.Name == name)
            {
                return section;
            }
        }
        return null;
    }

    private int CountDiagnostics(LfcDiagnosticSeverity severity) =>
        ListItemBuilder.CountDiagnostics(_session.LoadDiagnostics, severity);
}
