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
    public void Pop_ReturnsNewestFirst()
    {
        var history = new EditHistory();
        history.Push("a");
        history.Push("b");

        Assert.Equal("b", history.Pop());
        Assert.Equal("a", history.Pop());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Pop_Empty_Throws()
    {
        var history = new EditHistory();
        history.Push("a");
        history.Pop();

        Assert.Throws<InvalidOperationException>(() => history.Pop());
    }

    [Fact]
    public void Push_SameTextTwice_KeepsBoth()
    {
        var history = new EditHistory();
        history.Push("a");
        history.Push("a");

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void Push_EmptyText_IsKept()
    {
        // 空の文書（ToText() が ""）も、元に戻す先として正しい状態
        var history = new EditHistory();
        history.Push("");

        Assert.Equal("", history.Pop());
    }

    [Fact]
    public void Push_OverMaxCount_DropsOldest()
    {
        var history = new EditHistory();
        for (var i = 0; i <= EditHistory.MaxCount; i++)   // 上限より1件多く積む
        {
            history.Push(i.ToString());
        }

        Assert.Equal(EditHistory.MaxCount, history.Count);
        var popped = new List<string>();
        while (history.CanUndo)
        {
            popped.Add(history.Pop());
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
    public void Clear_RemovesAll()
    {
        var history = new EditHistory();
        history.Push("a");
        history.Push("b");

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.Throws<InvalidOperationException>(() => history.Pop());
    }

    [Fact]
    public void Push_Null_Throws()
    {
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new EditHistory().Push(null!)).ParamName);
    }
}
