namespace FileParseEditor.Core.Models;

/// <summary>
/// 値の表記を解釈できなかった理由（基本設計書 5-2）。利用者に見せる文言は ViewModel が決める。
/// </summary>
public enum ValueNotationError
{
    /// <summary>解釈できた（警告付きを含む）。</summary>
    None,

    /// <summary>改行（CR・LF）を含む。</summary>
    LineBreak,

    /// <summary>LFC の値として正しくない記述がある（閉じていない引用符など。読み込みであれば Error になる記述）。</summary>
    Syntax,

    /// <summary>クォートの外の # がコメントとして扱われた。</summary>
    Comment,
}
