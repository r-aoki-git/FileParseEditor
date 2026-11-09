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

    private SectionListItem? _selectedSection;
    private KeyListItem? _selectedKey;
    private string _title = "";
    private string _statusText = "";
    private string _previewText = "";

    public MainViewModel(IFileDialogService fileDialogService, IMessageService messageService)
    {
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(messageService);
        _fileDialogService = fileDialogService;
        _messageService = messageService;

        NewCommand = new RelayCommand(New);
        OpenCommand = new RelayCommand(Open);

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
                RefreshKeys();
            }
        }
    }

    /// <summary>選択中のセクションのキー一覧。</summary>
    public ObservableCollection<KeyListItem> Keys { get; } = new();

    /// <summary>選択中のキー。</summary>
    public KeyListItem? SelectedKey
    {
        get => _selectedKey;
        set => SetProperty(ref _selectedKey, value);
    }

    /// <summary>ファイルを開いた時点の診断情報の一覧。</summary>
    public ObservableCollection<DiagnosticListItem> Diagnostics { get; } = new();

    /// <summary>新規作成（基本設計書 6-2）。</summary>
    public ICommand NewCommand { get; }

    /// <summary>ファイルを開くダイアログで選んだファイルを開く（基本設計書 6-1）。</summary>
    public ICommand OpenCommand { get; }

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
        _messageService.ShowError(MessageTexts.UnexpectedError(exception.Message));
    }

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
            _messageService.ShowError(MessageTexts.OpenFailed(path, ex.Message));
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

    /// <summary>文書を置き換えた後に、全ての表示を更新する。セクションは先頭（グローバルセクション）を選択する。</summary>
    private void RefreshAll()
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
        _selectedSection = Sections[0];
        OnPropertyChanged(nameof(SelectedSection));
        RefreshKeys();

        Diagnostics.Clear();
        foreach (var diagnostic in _session.LoadDiagnostics)
        {
            Diagnostics.Add(DiagnosticListItem.From(diagnostic));
        }

        RefreshDocumentState();
    }

    private void RefreshKeys()
    {
        Keys.Clear();
        SelectedKey = null;

        var section = SelectedSection;
        if (section is null || !SectionExists(section.Name))
        {
            // グローバルセクションは、文書に存在しない場合も一覧に表示するため、キーは0件として扱う
            return;
        }

        var document = _session.Document;
        foreach (var key in document.Keys(section.Name))
        {
            Keys.Add(KeyListItem.From(key, document.Get(section.Name, key)));
        }
    }

    /// <summary>タイトル・ステータスバー・保存イメージを更新する。</summary>
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
    }

    private bool SectionExists(string name)
    {
        foreach (var section in _session.Document.Sections)
        {
            if (section == name)
            {
                return true;
            }
        }
        return false;
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
