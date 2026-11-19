using System.ComponentModel;
using System.Windows;
using FileParseEditor.Core.ViewModels;

namespace FileParseEditor.Views;

/// <summary>
/// メイン画面。表示と操作は <see cref="MainViewModel"/> にバインドし、
/// コードビハインドには画面固有の処理（ドラッグ＆ドロップ、終了）だけを書く（非機能要件 No.2）。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // 保存していない変更の確認が済み、閉じてよいと分かったか
    private bool _closeConfirmed;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // 保存イメージの TextBox などがドロップを先に処理しないよう、Preview イベントで受け付ける
    private void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    // イベントハンドラーは戻り値を返せないため async void とする。読み込み中も画面は止まらない
    private async void OnPreviewDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            await ViewModel.OpenDroppedFilesAsync(paths);
        }
    }

    private void OnExitMenuItemClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// 閉じる前に、保存していない変更を確認する（基本設計書 8-5）。
    /// 確認と保存は非同期のため、いったん閉じるのを取り消し、閉じてよいと分かってから改めて閉じる。
    /// </summary>
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed)
        {
            return;
        }

        e.Cancel = true;
        if (await ViewModel.ConfirmCloseWindowAsync())
        {
            _closeConfirmed = true;
            Close();
        }
    }
}
