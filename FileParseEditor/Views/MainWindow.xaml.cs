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

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // 保存イメージの TextBox などがドロップを先に処理しないよう、Preview イベントで受け付ける
    private void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnPreviewDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            ViewModel.OpenDroppedFiles(paths);
        }
        e.Handled = true;
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();
}
