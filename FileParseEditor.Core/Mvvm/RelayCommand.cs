namespace FileParseEditor.Core.Mvvm;

/// <summary>
/// 実行する処理と、実行できる条件を受け取るコマンド（すぐに終わる処理用）。
/// </summary>
internal sealed class RelayCommand : CommandBase
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <param name="execute">実行する処理。</param>
    /// <param name="canExecute">実行できる条件。省略した場合は常に実行できる。</param>
    internal RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>実行できる条件を満たしている場合だけ、処理を実行する。</summary>
    public override void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute();
        }
    }
}
