using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FileParseEditor.Core.Mvvm;

/// <summary>
/// 中身をまとめて入れ替え、変更の通知を1回にする <see cref="ObservableCollection{T}"/>。
/// ObservableCollection は要素を1つ追加・削除するたびに通知し、そのたびに画面が描き直されるため、
/// 一覧を作り直すときはこのクラスの <see cref="ReplaceAll"/> を使う。
/// </summary>
internal sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    // Count と、インデクサー（"Item[]" は WPF が使う名前）の変更通知の引数。毎回作らずに共有する
    private static readonly PropertyChangedEventArgs CountChanged = new(nameof(Count));
    private static readonly PropertyChangedEventArgs IndexerChanged = new("Item[]");
    private static readonly NotifyCollectionChangedEventArgs CollectionReset = new(NotifyCollectionChangedAction.Reset);

    /// <summary>
    /// 中身を <paramref name="items"/> に入れ替え、「全体が変わった」（Reset）の通知を1回だけ行う。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> が null。</exception>
    internal void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        // 通知の処理中に中身を変えようとした場合は、ObservableCollection と同じく例外にする
        CheckReentrancy();

        // Items（基底クラスが持つ内部のリスト）を直接変更すると、要素ごとの通知は発生しない
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnPropertyChanged(CountChanged);
        OnPropertyChanged(IndexerChanged);
        OnCollectionChanged(CollectionReset);
    }
}
