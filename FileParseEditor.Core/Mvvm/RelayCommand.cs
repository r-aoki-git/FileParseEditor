using System.Windows.Input;

namespace FileParseEditor.Core.Mvvm;

/// <summary>
/// 実行する処理と、実行できる条件を受け取る <see cref="ICommand"/> の実装。
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <param name="execute">実行する処理。</param>
    /// <param name="canExecute">実行できる条件。省略した場合は常に実行できる。</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>実行できる条件を満たしている場合だけ、処理を実行する。</summary>
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute();
        }
    }

    /// <summary>実行できる条件が変わったことを画面に通知する（ボタン・メニューの有効／無効が更新される）。</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
