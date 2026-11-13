using FileParseEditor.Core.Services;

namespace FileParseEditor.Tests.Helpers;

/// <summary>
/// テスト用のメッセージ。画面を表示せず、表示しようとしたメッセージを記録する。
/// 確認（Confirm）には、あらかじめ積んだ答えを順に返す。
/// </summary>
internal sealed class FakeMessageService : IMessageService
{
    internal List<string> Errors { get; } = new();

    internal List<string> Informations { get; } = new();

    internal List<string> Confirmations { get; } = new();

    /// <summary>Confirm が返す答え（true は「はい」）。空の状態で確認された場合はテストの誤りとして例外にする。</summary>
    internal Queue<bool> ConfirmAnswers { get; } = new();

    public void ShowError(string message) => Errors.Add(message);

    public void ShowInformation(string message) => Informations.Add(message);

    public bool Confirm(string message)
    {
        Confirmations.Add(message);
        return ConfirmAnswers.Count > 0
            ? ConfirmAnswers.Dequeue()
            : throw new InvalidOperationException($"想定していない確認です: {message}");
    }
}
