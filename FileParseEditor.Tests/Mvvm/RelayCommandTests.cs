using FileParseEditor.Core.Mvvm;

namespace FileParseEditor.Tests.Mvvm;

public class RelayCommandTests
{
    [Fact]
    public void Execute_WithoutCondition_RunsAction()
    {
        var count = 0;
        var command = new RelayCommand(() => count++);

        Assert.True(command.CanExecute(null));
        command.Execute(null);

        Assert.Equal(1, count);
    }

    [Fact]
    public void Execute_ConditionFalse_DoesNotRunAction()
    {
        // ショートカットキーなど、画面の無効化を経由せずに呼ばれた場合も実行しない
        var count = 0;
        var command = new RelayCommand(() => count++, () => false);

        Assert.False(command.CanExecute(null));
        command.Execute(null);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CanExecute_EvaluatesConditionEachTime()
    {
        var enabled = false;
        var command = new RelayCommand(() => { }, () => enabled);

        Assert.False(command.CanExecute(null));
        enabled = true;
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_NotifiesSubscribers()
    {
        var command = new RelayCommand(() => { });
        object? sender = null;
        command.CanExecuteChanged += (s, _) => sender = s;

        command.RaiseCanExecuteChanged();

        Assert.Same(command, sender);
    }

    [Fact]
    public void NullAction_Throws()
    {
        Assert.Equal("execute", Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!)).ParamName);
    }
}
