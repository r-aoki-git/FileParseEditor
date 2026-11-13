namespace FileParseEditor.Core.ViewModels;

/// <summary>
/// メッセージの文言（基本設計書 10章）。文言を1か所にまとめ、画面とテストで同じものを使う。
/// </summary>
internal static class MessageTexts
{
    /// <summary>M02：エラーを含むファイルを開いたとき。</summary>
    internal static string FileHasErrors(int errorCount) =>
        $"ファイルにエラーが {errorCount} 件あります。エラーの行は読み込んでいません。詳しくは診断情報の一覧を確認してください。";

    /// <summary>M04：セクションを削除するとき。</summary>
    internal static string ConfirmRemoveSection(string sectionDisplayName, int keyCount) =>
        $"セクション {sectionDisplayName} と、含まれるキー {keyCount} 件を削除しますか？";

    /// <summary>M05：入力した値に Warning となる記述があるとき。</summary>
    internal static string ConfirmValueWarnings(IReadOnlyList<string> warnings, string interpretedNotation) =>
        $"入力した値には次の注意点があります。\n{string.Join("\n", warnings)}\n\n次のように解釈します。\n{interpretedNotation}\n\nこのまま確定しますか？";

    /// <summary>M06：入力した値に Error となる記述があるとき。</summary>
    internal static string ValueSyntaxError(string detail) =>
        $"入力した値を解釈できません。\n{detail}";

    /// <summary>M07：入力した値に改行があるとき。</summary>
    internal const string ValueHasLineBreak =
        "値には改行を入力できません。文字列の中で改行する場合は \\n と入力してください。";

    /// <summary>M08：入力した値にクォートの外の # があるとき。</summary>
    internal const string ValueHasComment =
        "値にはコメント（#）を入力できません。# を文字の一部として使う場合は、\" \" で囲んで文字列にしてください。";

    /// <summary>M09：既に存在するキー名で追加しようとしたとき。</summary>
    internal static string KeyAlreadyExists(string key) => $"キー {key} は既に存在します。";

    /// <summary>M09：既に存在するセクション名で追加しようとしたとき。</summary>
    internal static string SectionAlreadyExists(string sectionDisplayName) => $"セクション {sectionDisplayName} は既に存在します。";

    /// <summary>M10：名前が空のとき。</summary>
    internal const string NameRequired = "名前を入力してください。";

    /// <summary>M10：LFC として書き出せない名前を指定したとき。</summary>
    internal static string InvalidName(string detail) => $"この名前は使用できません。\n{detail}";

    /// <summary>M11：ファイルの読み込みに失敗したとき。</summary>
    internal static string OpenFailed(string path, string detail) =>
        $"ファイルを開けませんでした。\n{path}\n{detail}";

    /// <summary>M13：複数のファイル・フォルダをドロップしたとき。</summary>
    internal const string DropOneFileOnly = "開けるのはファイル1つだけです。";

    /// <summary>
    /// 例外のメッセージを、メッセージの「詳細」として表示する形にする。
    /// <see cref="ArgumentException"/> のメッセージの末尾に .NET が付ける「 (Parameter 'xxx')」は、利用者には意味がないため取り除く。
    /// </summary>
    internal static string DetailOf(Exception exception)
    {
        var message = exception.Message;
        if (exception is ArgumentException { ParamName: { } paramName })
        {
            var suffix = $" (Parameter '{paramName}')";
            if (message.EndsWith(suffix, StringComparison.Ordinal))
            {
                return message[..^suffix.Length];
            }
        }
        return message;
    }

    /// <summary>M14：想定外の例外が発生したとき。</summary>
    internal static string UnexpectedError(string detail) =>
        $"予期しないエラーが発生しました。\n{detail}";
}
