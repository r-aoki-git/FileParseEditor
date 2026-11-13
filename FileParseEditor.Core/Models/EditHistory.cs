namespace FileParseEditor.Core.Models;

/// <summary>
/// 元に戻す履歴（基本設計書 6-9）。編集する直前の文書の ToText() の結果を、新しい順に最大 <see cref="MaxCount"/> 件保持する。
/// </summary>
public sealed class EditHistory
{
    /// <summary>保持する履歴の上限。超えた場合は古いものから捨てる。</summary>
    public const int MaxCount = 100;

    // 先頭が最も古く、末尾が最も新しい。件数が増減し、古いもの（先頭）も捨てるため、両端を O(1) で操作できる LinkedList とする
    private readonly LinkedList<string> _texts = new();

    /// <summary>保持している履歴の件数。</summary>
    public int Count => _texts.Count;

    /// <summary>元に戻せる履歴があるか。</summary>
    public bool CanUndo => _texts.Count > 0;

    /// <summary>編集する直前の文書のテキストを積む。</summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> が null。</exception>
    public void Push(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        _texts.AddLast(text);
        if (_texts.Count > MaxCount)
        {
            _texts.RemoveFirst();
        }
    }

    /// <summary>最後に積んだテキストを取り出す。</summary>
    /// <exception cref="InvalidOperationException">履歴がない。</exception>
    public string Pop()
    {
        var last = _texts.Last ?? throw new InvalidOperationException("元に戻せる履歴がありません。");
        _texts.RemoveLast();
        return last.Value;
    }

    /// <summary>全ての履歴を消去する（ファイルを開く・新規作成のとき）。</summary>
    public void Clear() => _texts.Clear();
}
