using FileParseEditor.Core.Mvvm;

namespace FileParseEditor.Tests.Mvvm;

/// <summary>AsyncRelayCommand：非同期の処理を実行するコマンドのテスト（基本設計書 7-6）。</summary>
public class AsyncRelayCommandTests
{
    [Fact]
    public async Task ExecuteAsync_RunsAction()
    {
        var count = 0;
        var command = new AsyncRelayCommand(() => { count++; return Task.CompletedTask; });

        await command.ExecuteAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WhileRunning_CannotExecute_AndSecondExecutionIsIgnored()
    {
        var gate = new TaskCompletionSource();
        var count = 0;
        var command = new AsyncRelayCommand(async () => { count++; await gate.Task; });

        var first = command.ExecuteAsync();
        Assert.True(command.IsRunning);
        Assert.False(command.CanExecute(null));

        await command.ExecuteAsync();      // 実行中に重ねて実行しようとしても、何もしない
        Assert.Equal(1, count);

        gate.SetResult();
        await first;
        Assert.False(command.IsRunning);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task NotifiesCanExecuteChanged_WhenStartedAndFinished()
    {
        var gate = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => gate.Task);
        var states = new List<bool>();
        command.CanExecuteChanged += (_, _) => states.Add(command.CanExecute(null));

        var running = command.ExecuteAsync();
        gate.SetResult();
        await running;

        Assert.Equal(new[] { false, true }, states);
    }

    [Fact]
    public async Task ConditionFalse_DoesNotRun()
    {
        var count = 0;
        var command = new AsyncRelayCommand(() => { count++; return Task.CompletedTask; }, () => false);

        Assert.False(command.CanExecute(null));
        await command.ExecuteAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Exception_IsPropagated_AndCommandCanRunAgain()
    {
        var command = new AsyncRelayCommand(() => throw new InvalidOperationException("失敗"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync());

        Assert.False(command.IsRunning);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void NullAction_Throws()
    {
        Assert.Equal("execute", Assert.Throws<ArgumentNullException>(() => new AsyncRelayCommand(null!)).ParamName);
    }
}
