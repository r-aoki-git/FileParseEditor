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
/// 文書の操作は <see cref="DocumentSession"/> に任せ、このクラスは表示内容の更新と、確認・メッセージの流れを担う。
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    internal const string ApplicationName = "FileParseEditor";
    internal const string UntitledName = "無題";

    private readonly DocumentSession _session = new();
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageService _messageService;
    private readonly IInputDialogService _inputDialogService;
    private readonly ValueInputChecker _valueInputChecker;

    // 実行できる条件が選択や文書の状態で変わるコマンド。状態が変わるたびに画面へ通知する
    private readonly RelayCommand[] _conditionalCommands;

    private SectionListItem? _selectedSection;
    private KeyListItem? _selectedKey;
    private string _editingValueText = "";
    private string _title = "";
    private string _statusText = "";
    private string _previewText = "";

    public MainViewModel(IFileDialogService fileDialogService, IMessageService messageService, IInputDialogService inputDialogService)
    {
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(messageService);
        ArgumentNullException.ThrowIfNull(inputDialogService);
        _fileDialogService = fileDialogService;
        _messageService = messageService;
        _inputDialogService = inputDialogService;
        _valueInputChecker = new ValueInputChecker(messageService);

        NewCommand = new RelayCommand(New);
        OpenCommand = new RelayCommand(Open);
        AddSectionCommand = new RelayCommand(AddSection);

        var undo = new RelayCommand(Undo, () => _session.CanUndo);
        var removeSection = new RelayCommand(RemoveSection, () => SelectedSection is not null && _session.SectionExists(SelectedSection.Name));
        var addKey = new RelayCommand(AddKey, () => SelectedSection is not null);
        var removeKey = new RelayCommand(RemoveKey, () => SelectedKey is not null);
        var applyValue = new RelayCommand(ApplyValue, () => SelectedKey is not null);
        UndoCommand = undo;
        RemoveSectionCommand = removeSection;
        AddKeyCommand = addKey;
        RemoveKeyCommand = removeKey;
        ApplyValueCommand = applyValue;
        _conditionalCommands = new[] { undo, removeSection, addKey, removeKey, applyValue };

        RefreshAll();
    }

    /// <summary>タイトルバーの表示（「FileParseEditor - ファイル名」。変更ありの場合は末尾に「 *」）。</summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    /// <summary>ステータスバーの表示（ファイルのパス、セクション・警告・エラーの件数）。</summary>
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

    /// <summary>セクション一覧。先頭は常にグローバルセクション（基本設計書 3-4-1）。</summary>
    public ObservableCollection<SectionListItem> Sections { get; } = new();

    /// <summary>選択中のセクション。変えるとキー一覧が更新される。</summary>
    public SectionListItem? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                RefreshKeys(null);
            }
        }
    }

    /// <summary>選択中のセクションのキー一覧。</summary>
    public ObservableCollection<KeyListItem> Keys { get; } = new();

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
    public ObservableCollection<DiagnosticListItem> Diagnostics { get; } = new();

    /// <summary>新規作成（基本設計書 6-2）。</summary>
    public ICommand NewCommand { get; }

    /// <summary>ファイルを開くダイアログで選んだファイルを開く（基本設計書 6-1）。</summary>
    public ICommand OpenCommand { get; }

    /// <summary>直前の編集を元に戻す（基本設計書 6-9）。元に戻せる履歴がある場合に実行できる。</summary>
    public ICommand UndoCommand { get; }

    /// <summary>セクションの追加（基本設計書 6-6）。</summary>
    public ICommand AddSectionCommand { get; }

    /// <summary>選択中のセクションの削除（基本設計書 6-7）。セクションが文書に存在する場合に実行できる。</summary>
    public ICommand RemoveSectionCommand { get; }

    /// <summary>選択中のセクションへのキーの追加（基本設計書 6-4）。セクションを選択している場合に実行できる。</summary>
    public ICommand AddKeyCommand { get; }

    /// <summary>選択中のキーの削除（基本設計書 6-5）。キーを選択している場合に実行できる。</summary>
    public ICommand RemoveKeyCommand { get; }

    /// <summary>値の編集欄の入力で、選択中のキーの値を変更する（基本設計書 6-3）。キーを選択している場合に実行できる。</summary>
    public ICommand ApplyValueCommand { get; }

    /// <summary>
    /// ドラッグ＆ドロップされたファイルを開く（基本設計書 6-1）。
    /// ファイル1つの場合だけ開き、複数のファイル・フォルダの場合はメッセージを表示する。
    /// </summary>
    public void OpenDroppedFiles(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Count != 1 || Directory.Exists(paths[0]))
        {
            _messageService.ShowError(MessageTexts.DropOneFileOnly);
            return;
        }
        OpenFile(paths[0]);
    }

    /// <summary>
    /// 想定外の例外を利用者に知らせる（基本設計書 9章）。アプリ全体の例外ハンドラーから呼び出す。
    /// </summary>
    public void ReportUnexpectedError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _messageService.ShowError(MessageTexts.UnexpectedError(MessageTexts.DetailOf(exception)));
    }

    // ---- ファイル ----

    private void New()
    {
        _session.New();
        RefreshAll();
    }

    private void Open()
    {
        var path = _fileDialogService.ShowOpenDialog();
        if (path is null)
        {
            return;
        }
        OpenFile(path);
    }

    private void OpenFile(string path)
    {
        try
        {
            _session.Open(path);
        }
        catch (Exception ex) when (IsFileAccessError(ex))
        {
            // 読み込みに失敗した場合は、現在の文書をそのまま残す
            _messageService.ShowError(MessageTexts.OpenFailed(path, MessageTexts.DetailOf(ex)));
            return;
        }

        RefreshAll();

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

    private void ApplyValue()
    {
        var section = SelectedSection;
        var key = SelectedKey;
        if (section is null || key is null)
        {
            return;
        }

        var result = LfcValueNotation.Parse(EditingValueText);
        if (!_valueInputChecker.Accept(result))
        {
            return;   // 入力は値の編集欄に残す
        }

        var current = _session.Document.Get(section.Name, key.Key);
        if (current.SequenceEqual(result.Values))
        {
            // 値が変わらない場合は変更として扱わない（表記だけを正規化して表示する）
            EditingValueText = LfcValueNotation.Format(result.Values);
            return;
        }

        _session.SetValue(section.Name, key.Key, result.Values);
        RefreshKeys(key.Key);
        RefreshDocumentState();
    }

    private void AddKey()
    {
        var section = SelectedSection;
        if (section is null)
        {
            return;
        }

        // 入力に問題があった場合は、メッセージの後に入力を残したままダイアログを再表示する（基本設計書 3-5-2）
        var input = NewKeyInput.Empty;
        while (true)
        {
            var answer = _inputDialogService.AskNewKey(section.DisplayName, input);
            if (answer is null)
            {
                return;
            }
            input = answer;

            if (input.Key.Length == 0)
            {
                _messageService.ShowError(MessageTexts.NameRequired);
                continue;
            }
            if (_session.KeyExists(section.Name, input.Key))
            {
                _messageService.ShowError(MessageTexts.KeyAlreadyExists(input.Key));
                continue;
            }

            var result = LfcValueNotation.Parse(input.ValueText);
            if (!_valueInputChecker.Accept(result))
            {
                continue;
            }

            try
            {
                _session.AddKey(section.Name, input.Key, result.Values);
            }
            catch (ArgumentException ex)
            {
                _messageService.ShowError(MessageTexts.InvalidName(MessageTexts.DetailOf(ex)));
                continue;
            }
            break;
        }

        RefreshKeys(input.Key);
        RefreshDocumentState();
    }

    private void RemoveKey()
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
        RefreshKeys(null);

        // 削除したキーの次のキー（末尾だった場合は前のキー）を選択する
        if (Keys.Count > 0)
        {
            SelectKey(Keys[Math.Min(index, Keys.Count - 1)]);
        }
        RefreshDocumentState();
    }

    private void AddSection()
    {
        var name = "";
        while (true)
        {
            var answer = _inputDialogService.AskSectionName(name);
            if (answer is null)
            {
                return;
            }
            name = answer;

            // グローバルセクションは常に一覧にあるため、追加の対象としない（基本設計書 6-6）
            if (name.Length == 0)
            {
                _messageService.ShowError(MessageTexts.NameRequired);
                continue;
            }
            if (_session.SectionExists(name))
            {
                _messageService.ShowInformation(MessageTexts.SectionAlreadyExists(SectionListItem.From(name).DisplayName));
                SelectedSection = FindSection(name);
                return;
            }

            try
            {
                _session.AddSection(name);
            }
            catch (ArgumentException ex)
            {
                _messageService.ShowError(MessageTexts.InvalidName(MessageTexts.DetailOf(ex)));
                continue;
            }
            break;
        }

        RefreshAll(name, null);
    }

    private void RemoveSection()
    {
        var section = SelectedSection;
        if (section is null || !_session.SectionExists(section.Name))
        {
            return;
        }

        var keyCount = _session.Document.Keys(section.Name).Count;
        if (!_messageService.Confirm(MessageTexts.ConfirmRemoveSection(section.DisplayName, keyCount)))
        {
            return;
        }

        _session.RemoveSection(section.Name);

        // グローバルセクションは削除しても一覧に残るため、選択したままにする
        RefreshAll(section.Name, null);
    }

    private void Undo()
    {
        if (!_session.CanUndo)
        {
            return;
        }

        // 元に戻した後も、同じセクション・キーがあれば選択し直す（基本設計書 6-9）
        var sectionName = SelectedSection?.Name;
        var keyName = SelectedKey?.Key;
        _session.Undo();
        RefreshAll(sectionName, keyName);
    }

    // ---- 表示の更新 ----

    /// <summary>
    /// 全ての表示を更新する。指定した名前のセクション・キーがあれば選択し、なければ先頭（グローバルセクション）を選択する。
    /// </summary>
    private void RefreshAll(string? sectionName = null, string? keyName = null)
    {
        Sections.Clear();
        Sections.Add(SectionListItem.From(LfcDocument.GlobalSectionName));
        foreach (var name in _session.Document.Sections)
        {
            // グローバルセクションは常に先頭に追加済み
            if (name != LfcDocument.GlobalSectionName)
            {
                Sections.Add(SectionListItem.From(name));
            }
        }

        // 同じ内容の行（record は値で比較される）を選び直した場合も、キー一覧は必ず作り直す
        _selectedSection = (sectionName is null ? null : FindSection(sectionName)) ?? Sections[0];
        OnPropertyChanged(nameof(SelectedSection));
        RefreshKeys(keyName);

        Diagnostics.Clear();
        foreach (var diagnostic in _session.LoadDiagnostics)
        {
            Diagnostics.Add(DiagnosticListItem.From(diagnostic));
        }

        RefreshDocumentState();
    }

    /// <summary>選択中のセクションのキー一覧を作り直す。指定した名前のキーがあれば選択する。</summary>
    private void RefreshKeys(string? keyName)
    {
        Keys.Clear();

        var section = SelectedSection;
        if (section is not null && _session.SectionExists(section.Name))
        {
            // グローバルセクションは、文書に存在しない場合も一覧に表示するため、その場合はキーを0件として扱う
            var document = _session.Document;
            foreach (var key in document.Keys(section.Name))
            {
                Keys.Add(KeyListItem.From(key, document.Get(section.Name, key)));
            }
        }

        KeyListItem? selected = null;
        foreach (var item in Keys)
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
        RaiseCanExecuteChanged();
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
        RaiseCanExecuteChanged();
    }

    private void RaiseCanExecuteChanged()
    {
        foreach (var command in _conditionalCommands)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private SectionListItem? FindSection(string name)
    {
        foreach (var section in Sections)
        {
            if (section.Name == name)
            {
                return section;
            }
        }
        return null;
    }

    private int CountDiagnostics(LfcDiagnosticSeverity severity)
    {
        var count = 0;
        foreach (var diagnostic in _session.LoadDiagnostics)
        {
            if (diagnostic.Severity == severity)
            {
                count++;
            }
        }
        return count;
    }
}
