using System.Collections.Specialized;
using FileParseEditor.Core.Mvvm;

namespace FileParseEditor.Tests.Mvvm;

/// <summary>BulkObservableCollection：まとめて入れ替えたときの変更通知のテスト（基本設計書 7-7）。</summary>
public class BulkObservableCollectionTests
{
    [Fact]
    public void ReplaceAll_NotifiesCollectionChangedOnlyOnce_AsReset()
    {
        var collection = new BulkObservableCollection<int> { 1, 2 };
        var events = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => events.Add(e.Action);

        collection.ReplaceAll(new[] { 3, 4, 5, 6 });

        // ObservableCollection の Clear と Add では 1 + 4 = 5 回通知される
        Assert.Equal(new[] { NotifyCollectionChangedAction.Reset }, events);
        Assert.Equal(new[] { 3, 4, 5, 6 }, collection);
    }

    [Fact]
    public void ReplaceAll_NotifiesCountAndIndexerOnce()
    {
        var collection = new BulkObservableCollection<int> { 1 };
        var properties = new List<string?>();
        ((System.ComponentModel.INotifyPropertyChanged)collection).PropertyChanged += (_, e) => properties.Add(e.PropertyName);

        collection.ReplaceAll(new[] { 1, 2, 3 });

        Assert.Equal(new[] { "Count", "Item[]" }, properties);
    }

    [Fact]
    public void ReplaceAll_Empty_ClearsWithOneNotification()
    {
        var collection = new BulkObservableCollection<int> { 1, 2, 3 };
        var count = 0;
        collection.CollectionChanged += (_, _) => count++;

        collection.ReplaceAll(Array.Empty<int>());

        Assert.Empty(collection);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ReplaceAll_SameCollectionInstanceAsSource_UsesSnapshot()
    {
        // 自分自身の中身を元に入れ替えても、要素が失われない（呼び出し側は配列で渡す）
        var collection = new BulkObservableCollection<int> { 1, 2 };

        collection.ReplaceAll(collection.ToArray());

        Assert.Equal(new[] { 1, 2 }, collection);
    }

    [Fact]
    public void ReplaceAll_DuringNotificationWithSeveralSubscribers_Throws()
    {
        // ObservableCollection と同じく、通知の処理中に中身を変えることはできない
        var collection = new BulkObservableCollection<int> { 1 };
        Exception? thrown = null;
        var reentered = false;
        collection.CollectionChanged += (_, _) => { };
        collection.CollectionChanged += (_, _) =>
        {
            // 確認は1回だけ行う（入れ替えができてしまった場合に、通知が繰り返されないようにする）
            if (reentered)
            {
                return;
            }
            reentered = true;
            thrown = Record.Exception(() => collection.ReplaceAll(new[] { 9 }));
        };

        collection.Add(2);

        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void ReplaceAll_Null_Throws()
    {
        Assert.Equal("items", Assert.Throws<ArgumentNullException>(() => new BulkObservableCollection<int>().ReplaceAll(null!)).ParamName);
    }
}
