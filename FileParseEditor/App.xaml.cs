using System.Windows;
using System.Windows.Threading;
using FileParseEditor.Core.ViewModels;
using FileParseEditor.Services;
using FileParseEditor.Views;

namespace FileParseEditor;

/// <summary>
/// 起動時に ViewModel とサービスを組み立て、メイン画面を表示する（基本設計書 7-3）。
/// </summary>
public partial class App : Application
{
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainViewModel = new MainViewModel(new FileDialogService(), new MessageService(), new InputDialogService());
        var window = new MainWindow { DataContext = _mainViewModel };
        MainWindow = window;
        window.Show();
    }

    /// <summary>
    /// 操作ごとに受け止めなかった想定外の例外を受け止め、メッセージを表示してアプリを続行する（基本設計書 9章、非機能要件 No.6）。
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_mainViewModel is null)
        {
            // 起動処理の途中で発生した場合は、続行できないため既定の動作（アプリの終了）に任せる
            return;
        }

        _mainViewModel.ReportUnexpectedError(e.Exception);
        e.Handled = true;
    }
}
