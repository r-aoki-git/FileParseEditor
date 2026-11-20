using System.Windows.Input;

namespace FileParseEditor.Core.Mvvm;

/// <summary>
/// コマンドの共通部分。実行できる条件が変わったことを画面に通知する処理を、同期・非同期のコマンドで共有する。
/// </summary>
internal abstract class CommandBase : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public abstract bool CanExecute(object? parameter);

    public abstract void Execute(object? parameter);

    /// <summary>実行できる条件が変わったことを画面に通知する（ボタン・メニューの有効／無効が更新される）。</summary>
    internal void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
