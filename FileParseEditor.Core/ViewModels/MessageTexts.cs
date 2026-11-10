namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// メッセージの文言（基本設計書 10章）。文言を1か所にまとめ、画面とテストで同じものを使う。
/// </summary>
internal static class MessageTexts
{
    /// <summary>M02：エラーを含むファイルを開いたとき。</summary>
    internal static string FileHasErrors(int errorCount) =>
        $"ファイルにエラーが {errorCount} 件あります。エラーの行は読み込んでいません。詳しくは診断情報の一覧を確認してください。";

    /// <summary>M11：ファイルの読み込みに失敗したとき。</summary>
    internal static string OpenFailed(string path, string detail) =>
        $"ファイルを開けませんでした。\n{path}\n{detail}";

    /// <summary>M13：複数のファイル・フォルダをドロップしたとき。</summary>
    internal const string DropOneFileOnly = "開けるのはファイル1つだけです。";

    /// <summary>M14：想定外の例外が発生したとき。</summary>
    internal static string UnexpectedError(string detail) =>
        $"予期しないエラーが発生しました。\n{detail}";
}
