using FileParseEditor.Core.Services;

namespace FileParseEditor.Tests.Helpers;

/// <summary>
/// テスト用の入力ダイアログ。画面を表示せず、あらかじめ積んだ入力を順に返し、表示しようとした初期値を記録する。
/// 入力が空の状態で表示された場合は、キャンセル（null）を返す。
/// </summary>
internal sealed class FakeInputDialogService : IInputDialogService
{
    internal Queue<string?> SectionNameAnswers { get; } = new();

    internal Queue<NewKeyInput?> NewKeyAnswers { get; } = new();

    /// <summary>セクションの追加ダイアログを表示したときの初期値（表示した回数分）。</summary>
    internal List<string> SectionNameInitialValues { get; } = new();

    /// <summary>キーの追加ダイアログを表示したときの、セクションの表示名と初期値（表示した回数分）。</summary>
    internal List<(string Section, NewKeyInput Input)> NewKeyRequests { get; } = new();

    public string? AskSectionName(string initialName)
    {
        SectionNameInitialValues.Add(initialName);
        return SectionNameAnswers.Count > 0 ? SectionNameAnswers.Dequeue() : null;
    }

    public NewKeyInput? AskNewKey(string sectionDisplayName, NewKeyInput initialInput)
    {
        NewKeyRequests.Add((sectionDisplayName, initialInput));
        return NewKeyAnswers.Count > 0 ? NewKeyAnswers.Dequeue() : null;
    }
}
