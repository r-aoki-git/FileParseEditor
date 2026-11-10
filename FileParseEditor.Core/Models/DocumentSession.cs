using FileParserLib;

namespace FileParseEditor.Core.Models;

/// <summary>
/// 編集中の文書と、ファイルのパス・読み込み時の診断情報・保存済みの内容をまとめて管理する（基本設計書 7-3、7-5）。
/// </summary>
public sealed class DocumentSession
{
    // エラーを含むファイルも開けるよう、例外を送出させずに読み込む（要件定義書 4-3）
    private static readonly LfcLoadOptions LoadOptions = new() { ThrowOnError = false };

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

    /// <summary>編集中の文書。</summary>
    public LfcDocument Document { get; private set; }

    /// <summary>開いたファイルの絶対パス。新規作成した文書は null。</summary>
    public string? FilePath { get; private set; }

    /// <summary>ファイルを開いた時点の診断情報。新規作成した文書は0件。</summary>
    public IReadOnlyList<LfcDiagnostic> LoadDiagnostics { get; private set; }

    /// <summary>現在の文書の ToText() の結果（保存イメージ・「変更あり」の判定に使う）。</summary>
    public string CurrentText { get; private set; }

    /// <summary>保存していない変更があるか（基本設計書 6-11）。</summary>
    public bool IsModified => CurrentText != _savedText;

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

    private void Replace(LfcDocument document, string? filePath, IReadOnlyList<LfcDiagnostic> diagnostics)
    {
        Document = document;
        FilePath = filePath;
        LoadDiagnostics = diagnostics;
        CurrentText = document.ToText();
        _savedText = CurrentText;
    }
}
