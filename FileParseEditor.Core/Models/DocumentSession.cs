using FileParserLib;

namespace FileParseEditor.Core.Models;

/// <summary>
/// 編集中の文書と、ファイルのパス・読み込み時の診断情報・保存済みの内容・元に戻す履歴をまとめて管理する（基本設計書 7-3、7-5）。
/// 文書を変更する操作は、このクラスを通して行う（直前の状態を元に戻す履歴に記録するため）。
/// </summary>
public sealed class DocumentSession
{
    // エラーを含むファイルも開けるよう、例外を送出させずに読み込む（要件定義書 4-3）
    private static readonly LfcLoadOptions LoadOptions = new() { ThrowOnError = false };

    private readonly EditHistory _history = new();

    // 開いた・新規作成した時点の ToText() の結果。「変更あり」の判定に使う（基本設計書 6-11）
    private string _savedText;

    /// <summary>空の文書（新規作成と同じ状態）で始める。</summary>
    public DocumentSession()
    {
        Document = LfcDocument.CreateEmpty();
        LoadDiagnostics = Array.Empty<LfcDiagnostic>();
        CurrentText = Document.ToText();
        _savedText = CurrentText;
    }

    /// <summary>編集中の文書。変更する場合は、このクラスの編集用のメソッドを使う。</summary>
    public LfcDocument Document { get; private set; }

    /// <summary>開いたファイルの絶対パス。新規作成した文書は null。</summary>
    public string? FilePath { get; private set; }

    /// <summary>ファイルを開いた時点の診断情報。新規作成した文書は0件。編集・元に戻すでは変わらない。</summary>
    public IReadOnlyList<LfcDiagnostic> LoadDiagnostics { get; private set; }

    /// <summary>現在の文書の ToText() の結果（保存イメージ・「変更あり」の判定に使う）。</summary>
    public string CurrentText { get; private set; }

    /// <summary>保存していない変更があるか（基本設計書 6-11）。元に戻して開いた時点と同じ内容になれば false に戻る。</summary>
    public bool IsModified => CurrentText != _savedText;

    /// <summary>元に戻せる履歴があるか。</summary>
    public bool CanUndo => _history.CanUndo;

    /// <summary>
    /// ファイルを開く（基本設計書 6-1）。エラーを含むファイルも例外にせずに開き、診断情報を <see cref="LoadDiagnostics"/> に保持する。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が null。</exception>
    /// <remarks>
    /// ファイルの読み込みに失敗した場合（ファイルがない、アクセス権がない等）は、ライブラリの例外がそのまま伝わり、現在の状態は変わらない。
    /// </remarks>
    public void Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var document = LfcDocument.Load(path, LoadOptions);
        Replace(document, Path.GetFullPath(path), document.Diagnostics);
    }

    /// <summary>空の文書を作る（基本設計書 6-2）。</summary>
    public void New() => Replace(LfcDocument.CreateEmpty(), null, Array.Empty<LfcDiagnostic>());

    /// <summary>セクションが文書に存在するか（グローバルセクションは、キーを持つ場合などに存在する）。</summary>
    public bool SectionExists(string section)
    {
        ArgumentNullException.ThrowIfNull(section);

        foreach (var name in Document.Sections)
        {
            if (name == section)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>キーが文書に存在するか。</summary>
    public bool KeyExists(string section, string key) => Document.TryGet(section, key, out _);

    /// <summary>
    /// 既存のキーの値を置き換える（基本設計書 6-3）。キーの位置とコメントは維持される。
    /// </summary>
    /// <exception cref="KeyNotFoundException">セクションまたはキーが存在しない。</exception>
    /// <exception cref="ArgumentException">値が0件、または LFC として書き出せない値（NaN 等）。文書は変わらない。</exception>
    public void SetValue(string section, string key, IReadOnlyList<LfcValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (!KeyExists(section, key))
        {
            throw new KeyNotFoundException($"セクション '{section}' にキー '{key}' がありません。");
        }

        var array = ToArray(values);
        Edit(document => document.Set(section, key, array));
    }

    /// <summary>
    /// キーをセクションの末尾に追加する（基本設計書 6-4）。セクションがなければ作成する。
    /// </summary>
    /// <exception cref="InvalidOperationException">同じ名前のキーが既にある（ライブラリの Set は上書きしてしまうため、追加としては扱わない）。</exception>
    /// <exception cref="ArgumentException">LFC として書き出せない名前・値。文書は変わらない。</exception>
    public void AddKey(string section, string key, IReadOnlyList<LfcValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (KeyExists(section, key))
        {
            throw new InvalidOperationException($"セクション '{section}' にキー '{key}' は既に存在します。");
        }

        var array = ToArray(values);
        Edit(document => document.Set(section, key, array));
    }

    /// <summary>キーを、付いていたコメントごと削除する（基本設計書 6-5）。</summary>
    /// <returns>削除した場合は true。キーがなかった場合は false（文書も履歴も変わらない）。</returns>
    public bool RemoveKey(string section, string key)
    {
        if (!KeyExists(section, key))
        {
            return false;
        }

        Edit(document => document.Remove(section, key));
        return true;
    }

    /// <summary>セクションを末尾に追加する（基本設計書 6-6）。</summary>
    /// <returns>追加した場合は true。既にあった場合は false（文書も履歴も変わらない）。</returns>
    /// <exception cref="ArgumentException">LFC として書き出せない名前。文書は変わらない。</exception>
    public bool AddSection(string section)
    {
        if (SectionExists(section))
        {
            return false;
        }

        Edit(document => document.AddSection(section));
        return true;
    }

    /// <summary>セクションを、含まれる全てのキーごと削除する（基本設計書 6-7）。</summary>
    /// <returns>削除した場合は true。セクションがなかった場合は false（文書も履歴も変わらない）。</returns>
    public bool RemoveSection(string section)
    {
        if (!SectionExists(section))
        {
            return false;
        }

        Edit(document => document.RemoveSection(section));
        return true;
    }

    /// <summary>
    /// 直前の編集を元に戻す（基本設計書 6-9）。履歴のテキストから文書を作り直すため、
    /// 削除したキー・セクションも、元の位置とコメントを含めて戻る。診断情報は変えない。
    /// </summary>
    /// <exception cref="InvalidOperationException">元に戻せる履歴がない。</exception>
    public void Undo()
    {
        var text = _history.Pop();
        Document = LfcDocument.Parse(text, LoadOptions);
        CurrentText = Document.ToText();
    }

    /// <summary>
    /// 現在の文書を履歴に記録してから変更する。ライブラリが例外を送出した場合（文書は変わらない）は、記録した履歴を取り消す。
    /// </summary>
    private void Edit(Action<LfcDocument> change)
    {
        _history.Push(CurrentText);
        try
        {
            change(Document);
        }
        catch
        {
            _history.Pop();
            throw;
        }
        CurrentText = Document.ToText();
    }

    private void Replace(LfcDocument document, string? filePath, IReadOnlyList<LfcDiagnostic> diagnostics)
    {
        Document = document;
        FilePath = filePath;
        LoadDiagnostics = diagnostics;
        CurrentText = document.ToText();
        _savedText = CurrentText;
        _history.Clear();
    }

    private static LfcValue[] ToArray(IReadOnlyList<LfcValue> values)
    {
        var array = new LfcValue[values.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = values[i];
        }
        return array;
    }
}
