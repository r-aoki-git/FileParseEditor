namespace FileParseEditor.Core.Mvvm;

/// <summary>
/// 時間のかかる処理（ファイルの読み込みなど）を非同期に実行するコマンド。
/// 処理の間は画面を止めず、同じコマンドが重ねて実行されないよう、実行できない状態にする。
/// </summary>
internal sealed class AsyncRelayCommand : CommandBase
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;

    /// <param name="execute">実行する非同期の処理。</param>
    /// <param name="canExecute">実行できる条件。省略した場合は、実行中でなければ常に実行できる。</param>
    internal AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>処理を実行中か。</summary>
    internal bool IsRunning { get; private set; }

    public override bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke() ?? true);

    /// <summary>
    /// 画面（ボタン・メニュー）から呼ばれる。ICommand の仕様上 Task を返せないため、完了を待たずに戻る。
    /// 処理中の例外は、画面のスレッドでアプリ全体の例外ハンドラーに伝わる。
    /// </summary>
    public override async void Execute(object? parameter) => await ExecuteAsync();

    /// <summary>実行できる条件を満たしている場合だけ処理を実行し、完了を待てる Task を返す。</summary>
    internal async Task ExecuteAsync()
    {
        if (!CanExecute(null))
        {
            return;
        }

        IsRunning = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            IsRunning = false;
            RaiseCanExecuteChanged();
        }
    }
}
