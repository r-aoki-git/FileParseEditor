using FileParseEditor.Core.Models;
using FileParseEditor.Core.Services;

namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// 値の入力の解釈結果を利用者に示し、確定してよいかを決める（基本設計書 6-3 の 2・3、6-4 の 3）。
/// 値の変更とキーの追加で同じ流れを使うため、<see cref="MainViewModel"/> から分けている。
/// </summary>
internal sealed class ValueInputChecker
{
    private readonly IMessageService _messageService;

    internal ValueInputChecker(IMessageService messageService)
    {
        ArgumentNullException.ThrowIfNull(messageService);
        _messageService = messageService;
    }

    /// <summary>
    /// 入力エラーの場合はメッセージを表示して false を返す。警告付きの場合は解釈した結果を示して確認し、
    /// 「はい」なら true を返す。それ以外は true を返す。
    /// </summary>
    internal bool Accept(ValueNotationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        switch (result.Error)
        {
            case ValueNotationError.None:
                break;
            case ValueNotationError.LineBreak:
                _messageService.ShowError(MessageTexts.ValueHasLineBreak);
                return false;
            case ValueNotationError.Comment:
                _messageService.ShowError(MessageTexts.ValueHasComment);
                return false;
            default:
                _messageService.ShowError(MessageTexts.ValueSyntaxError(result.ErrorDetail));
                return false;
        }

        if (!result.HasWarnings)
        {
            return true;
        }

        // 確定した値は保存時に正規化されるため、正規化後の表記を「解釈した結果」として示す（基本設計書 5-2）
        var interpreted = LfcValueNotation.Format(result.Values);
        return _messageService.Confirm(MessageTexts.ConfirmValueWarnings(result.Warnings, interpreted));
    }
}
