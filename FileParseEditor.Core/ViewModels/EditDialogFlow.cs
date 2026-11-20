using FileParseEditor.Core.Models;
using FileParseEditor.Core.Services;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// 入力ダイアログや確認を伴う編集操作の流れ（基本設計書 6-4、6-6、6-7）。
/// 入力に問題があればメッセージを表示してダイアログを再表示し、問題がなければ <see cref="DocumentSession"/> で文書を変更する。
/// 表示の更新は呼び出し元（<see cref="MainViewModel"/>）が行う。
/// </summary>
internal sealed class EditDialogFlow
{
    private readonly DocumentSession _session;
    private readonly IMessageService _messageService;
    private readonly IInputDialogService _inputDialogService;
    private readonly ValueInputChecker _valueInputChecker;

    internal EditDialogFlow(
        DocumentSession session,
        IMessageService messageService,
        IInputDialogService inputDialogService,
        ValueInputChecker valueInputChecker)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(messageService);
        ArgumentNullException.ThrowIfNull(inputDialogService);
        ArgumentNullException.ThrowIfNull(valueInputChecker);
        _session = session;
        _messageService = messageService;
        _inputDialogService = inputDialogService;
        _valueInputChecker = valueInputChecker;
    }

    /// <summary>
    /// キーの追加ダイアログで入力されたキーを、セクションの末尾に追加する（基本設計書 6-4）。
    /// 入力に問題があった場合は、メッセージの後に入力を残したままダイアログを再表示する。
    /// </summary>
    /// <returns>追加したキー名。キャンセルされた場合は null。</returns>
    internal string? AddKeyWithDialog(SectionListItem section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var input = NewKeyInput.Empty;
        while (true)
        {
            var answer = _inputDialogService.AskNewKey(section.DisplayName, input);
            if (answer is null)
            {
                return null;
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

            var result = LfcValueNotation.ParseNotation(input.ValueText);
            if (!_valueInputChecker.ConfirmValueInput(result))
            {
                continue;
            }

            try
            {
                _session.AddKey(section.Name, input.Key, result.Values);
                return input.Key;
            }
            catch (ArgumentException ex)
            {
                _messageService.ShowError(MessageTexts.InvalidName(MessageTexts.ExceptionDetail(ex)));
            }
        }
    }

    /// <summary>
    /// セクションの追加ダイアログで入力されたセクションを、末尾に追加する（基本設計書 6-6）。
    /// 既に存在する名前の場合は、その旨を表示して追加しない。
    /// </summary>
    /// <returns>選択するセクション名（追加したセクション、または既に存在したセクション）。キャンセルされた場合は null。</returns>
    internal string? AddSectionWithDialog()
    {
        var name = "";
        while (true)
        {
            var answer = _inputDialogService.AskNewSectionName(name);
            if (answer is null)
            {
                return null;
            }
            name = answer;

            // グローバルセクションは常に一覧にあるため、追加の対象としない
            if (name.Length == 0)
            {
                _messageService.ShowError(MessageTexts.NameRequired);
                continue;
            }
            if (_session.SectionExists(name))
            {
                _messageService.ShowInformation(MessageTexts.SectionAlreadyExists(SectionListItem.ForSection(name).DisplayName));
                return name;
            }

            try
            {
                _session.AddSection(name);
                return name;
            }
            catch (ArgumentException ex)
            {
                _messageService.ShowError(MessageTexts.InvalidName(MessageTexts.ExceptionDetail(ex)));
            }
        }
    }

    /// <summary>
    /// 含まれるキーの件数を示して確認し、「はい」ならセクションを削除する（基本設計書 6-7）。
    /// </summary>
    /// <returns>削除した場合は true。「いいえ」の場合、またはセクションが文書にない場合は false。</returns>
    internal bool RemoveSectionAfterConfirmation(SectionListItem section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (!_session.SectionExists(section.Name))
        {
            return false;
        }

        var keyCount = _session.Document.Keys(section.Name).Count;
        if (!_messageService.ConfirmYesNo(MessageTexts.ConfirmRemoveSection(section.DisplayName, keyCount)))
        {
            return false;
        }

        return _session.RemoveSection(section.Name);
    }
}
