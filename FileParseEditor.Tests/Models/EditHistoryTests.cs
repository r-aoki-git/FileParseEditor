using FileParseEditor.Core.Models;

namespace FileParseEditor.Tests.Models;

/// <summary>EditHistory：元に戻す履歴のテスト（基本設計書 6-9）。</summary>
public class EditHistoryTests
{
    [Fact]
    public void Initial_IsEmpty()
    {
        var history = new EditHistory();

        Assert.False(history.CanUndo);
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void TakeLatestText_ReturnsNewestFirst()
    {
        var history = new EditHistory();
        history.AddTextBeforeEdit("a");
        history.AddTextBeforeEdit("b");

        Assert.Equal("b", history.TakeLatestText());
        Assert.Equal("a", history.TakeLatestText());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void TakeLatestText_Empty_Throws()
    {
        var history = new EditHistory();
        history.AddTextBeforeEdit("a");
        history.TakeLatestText();

        Assert.Throws<InvalidOperationException>(() => history.TakeLatestText());
    }

    [Fact]
    public void AddTextBeforeEdit_SameTextTwice_KeepsBoth()
    {
        var history = new EditHistory();
        history.AddTextBeforeEdit("a");
        history.AddTextBeforeEdit("a");

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void AddTextBeforeEdit_EmptyText_IsKept()
    {
        // 空の文書（ToText() が ""）も、元に戻す先として正しい状態
        var history = new EditHistory();
        history.AddTextBeforeEdit("");

        Assert.Equal("", history.TakeLatestText());
    }

    [Fact]
    public void AddTextBeforeEdit_OverMaxCount_DropsOldest()
    {
        var history = new EditHistory();
        for (var i = 0; i <= EditHistory.MaxCount; i++)   // 上限より1件多く積む
        {
            history.AddTextBeforeEdit(i.ToString());
        }

        Assert.Equal(EditHistory.MaxCount, history.Count);
        var popped = new List<string>();
        while (history.CanUndo)
        {
            popped.Add(history.TakeLatestText());
        }
        Assert.Equal(EditHistory.MaxCount.ToString(), popped[0]);   // 最新は残る
        Assert.Equal("1", popped[^1]);                              // 最も古い "0" だけが捨てられる
    }

    [Fact]
    public void MaxCount_Is100()
    {
        Assert.Equal(100, EditHistory.MaxCount);
    }

    [Fact]
    public void ClearAll_RemovesAll()
    {
        var history = new EditHistory();
        history.AddTextBeforeEdit("a");
        history.AddTextBeforeEdit("b");

        history.ClearAll();

        Assert.False(history.CanUndo);
        Assert.Throws<InvalidOperationException>(() => history.TakeLatestText());
    }

    [Fact]
    public void AddTextBeforeEdit_Null_Throws()
    {
        Assert.Equal("textBeforeEdit", Assert.Throws<ArgumentNullException>(() => new EditHistory().AddTextBeforeEdit(null!)).ParamName);
    }
}
