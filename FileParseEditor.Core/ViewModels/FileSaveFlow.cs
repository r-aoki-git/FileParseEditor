using FileParseEditor.Core.Models;
using FileParseEditor.Core.Services;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// 保存と、保存していない変更の確認の流れ（基本設計書 6-8、6-10）。
/// 保存先の選択・エラーの行が失われる確認・保存の失敗のメッセージを担い、保存そのものは <see cref="DocumentSession"/> が行う。
/// 保存中の状態（操作を実行できなくする、ステータスバーの表示）と表示の更新は、呼び出し元から受け取った処理で行う。
/// </summary>
internal sealed class FileSaveFlow
{
    /// <summary>新規作成した文書を保存するときの、ファイル名の初期値。</summary>
    internal const string UntitledFileName = MainViewModel.UntitledName + ".lfc";

    private readonly DocumentSession _session;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageService _messageService;
    private readonly Func<string, Func<Task>, Task> _runFileOperationAsync;

    /// <param name="runFileOperationAsync">
    /// ファイルの読み書きを、操作を実行できない状態（基本設計書 7-6）で実行する処理。
    /// 引数は、ステータスバーに表示する文言と、実行する処理。
    /// </param>
    internal FileSaveFlow(
        DocumentSession session,
        IFileDialogService fileDialogService,
        IMessageService messageService,
        Func<string, Func<Task>, Task> runFileOperationAsync)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(messageService);
        ArgumentNullException.ThrowIfNull(runFileOperationAsync);
        _session = session;
        _fileDialogService = fileDialogService;
        _messageService = messageService;
        _runFileOperationAsync = runFileOperationAsync;
    }

    /// <summary>
    /// 上書き保存する（基本設計書 6-8）。新規作成した文書は、名前を付けて保存と同じ動作をする。
    /// </summary>
    /// <returns>保存した場合は true。キャンセルされた場合、保存に失敗した場合は false。</returns>
    internal Task<bool> SaveToCurrentFileAsync() =>
        _session.FilePath is null ? SaveWithFileDialogAsync() : SaveToPathAsync(_session.FilePath);

    /// <summary>
    /// ファイルを保存するダイアログで保存先を選んで保存する（基本設計書 6-8）。
    /// </summary>
    /// <returns>保存した場合は true。キャンセルされた場合、保存に失敗した場合は false。</returns>
    internal async Task<bool> SaveWithFileDialogAsync()
    {
        var path = _fileDialogService.ShowSaveFileDialog(_session.FilePath ?? UntitledFileName);
        if (path is null)
        {
            return false;
        }
        return await SaveToPathAsync(path);
    }

    /// <summary>
    /// 保存していない変更があれば、保存するかを確認する（基本設計書 6-10）。
    /// 「はい」の場合は上書き保存する。ファイルを開く・新規作成・終了の前に呼び出す。
    /// </summary>
    /// <returns>元の操作を続けてよい場合は true（変更がない、「いいえ」、または「はい」で保存できた）。中止する場合は false。</returns>
    internal async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!_session.IsModified)
        {
            return true;
        }

        var fileName = _session.FilePath is null ? MainViewModel.UntitledName : Path.GetFileName(_session.FilePath);
        return _messageService.AskSaveChanges(MessageTexts.ConfirmSaveChanges(fileName)) switch
        {
            SaveChangesChoice.Save => await SaveToCurrentFileAsync(),   // 保存がキャンセル・失敗した場合は、元の操作も中止する
            SaveChangesChoice.Discard => true,
            _ => false,
        };
    }

    private async Task<bool> SaveToPathAsync(string path)
    {
        // エラーを含む状態で開いた元のファイルに上書きする場合は、エラーの行が失われることを確認する（要件定義書 4-3）
        if (_session.NeedsErrorLossConfirmation(path) && !_messageService.ConfirmYesNo(MessageTexts.ConfirmErrorLinesLoss))
        {
            return false;
        }

        try
        {
            await _runFileOperationAsync($"保存中: {path}", () => _session.SaveFileAsync(path));
            return true;
        }
        catch (Exception ex) when (FileAccessErrors.IsRecoverable(ex))
        {
            // 保存に失敗した場合は、文書と「変更あり」の状態を変えない
            _messageService.ShowError(MessageTexts.SaveFailed(path, MessageTexts.ExceptionDetail(ex)));
            return false;
        }
    }
}
