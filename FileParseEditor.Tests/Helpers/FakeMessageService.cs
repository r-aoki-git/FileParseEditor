using FileParseEditor.Core.Services;

namespace FileParseEditor.Tests.Helpers;

/// <summary>
/// テスト用のメッセージ。画面を表示せず、表示しようとしたメッセージを記録する。
/// </summary>
internal sealed class FakeMessageService : IMessageService
{
    internal List<string> Errors { get; } = new();

    internal List<string> Informations { get; } = new();

    public void ShowError(string message) => Errors.Add(message);

    public void ShowInformation(string message) => Informations.Add(message);
}
