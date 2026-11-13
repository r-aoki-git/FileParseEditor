using FileParseEditor.Core.Models;
using FileParserLib;

namespace FileParseEditor.Tests.Models;

/// <summary>LfcValueNotation：表記 → 値（Parse）のテスト（基本設計書 5-2）。</summary>
public class LfcValueNotationParseTests
{
    // ---- 成功 ----

    public static TheoryData<string, LfcValue[]> SuccessCases => new()
    {
        { "1", new LfcValue[] { 1 } },
        { "1,\"tasiro\",2", new LfcValue[] { 1, "tasiro", 2 } },
        { "  1 ,  2  ", new LfcValue[] { 1, 2 } },                        // 前後の空白は無視する（ライブラリの規則）
        { "", new LfcValue[] { LfcValue.Null } },                         // 空の入力は値なし
        { "   ", new LfcValue[] { LfcValue.Null } },                      // 空白だけの入力も値なし
        { ",", new LfcValue[] { LfcValue.Null, LfcValue.Null } },         // 空の要素2つ
        { "01", new LfcValue[] { 1 } },                                   // 先頭の0は Int として解釈する
        { "1e3", new LfcValue[] { 1000.0 } },                             // 指数表記は Double
        { "1.0", new LfcValue[] { 1.0 } },
        { "TRUE", new LfcValue[] { true } },                              // 真偽値は大文字・小文字を区別しない
        { "\"\"", new LfcValue[] { "" } },                                // 空文字列は値なしと区別する
        { "\"a#b,c\"", new LfcValue[] { "a#b,c" } },                      // クォートの中の # と , は文字列の一部
        { "\"改行\\nあり\\tタブ\"", new LfcValue[] { "改行\nあり\tタブ" } }, // 改行・タブはエスケープで入力する
        { "\"C:\\\\temp\"", new LfcValue[] { "C:\\temp" } },
        { "\"123\"", new LfcValue[] { "123" } },                          // クォートした数字は String
    };

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public void Success_WithoutWarnings(string text, LfcValue[] expected)
    {
        var result = LfcValueNotation.Parse(text);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValueNotationError.None, result.Error);
        Assert.Equal("", result.ErrorDetail);
        Assert.False(result.HasWarnings);
        Assert.Equal(expected, result.Values);
    }

    // ---- 警告付きの成功（読み込みであれば Warning になる記述） ----

    [Theory]
    [InlineData("Auto", "Auto", "クォートなしの文字列です: Auto")]
    [InlineData("a=b", "a=b", "クォートなしの文字列です: a=b")]           // = は値の一部
    [InlineData("[x]", "[x]", "クォートなしの文字列です: [x]")]           // 値の位置ではセクションとして扱われない
    [InlineData("\"x\\d\"", "x\\d", "未定義のエスケープシーケンスです: \\d")]
    public void Success_WithWarning(string text, string expectedString, string expectedWarning)
    {
        var result = LfcValueNotation.Parse(text);

        Assert.True(result.IsSuccess);
        Assert.True(result.HasWarnings);
        Assert.Equal(new[] { expectedWarning }, result.Warnings);
        Assert.Equal(new LfcValue[] { expectedString }, result.Values);
    }

    [Fact]
    public void Success_WithWarningsForEachElement()
    {
        var result = LfcValueNotation.Parse("Auto,1,Manual");

        Assert.Equal(new[] { "クォートなしの文字列です: Auto", "クォートなしの文字列です: Manual" }, result.Warnings);
        Assert.Equal(new LfcValue[] { "Auto", 1, "Manual" }, result.Values);
    }

    // ---- 入力エラー ----

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("1\r\n")]
    [InlineData("\n")]
    public void LineBreak_IsError(string text)
    {
        // 改行を許すと、作業用の文書の中で別のキー・セクションとして扱われてしまう
        var result = LfcValueNotation.Parse(text);

        Assert.False(result.IsSuccess);
        Assert.Equal(ValueNotationError.LineBreak, result.Error);
        Assert.Empty(result.Values);
    }

    [Fact]
    public void LineBreakThatWouldAddAnotherKey_IsError()
    {
        Assert.Equal(ValueNotationError.LineBreak, LfcValueNotation.Parse("1\nOther = 2").Error);
        Assert.Equal(ValueNotationError.LineBreak, LfcValueNotation.Parse("1\n[Section]").Error);
    }

    [Theory]
    [InlineData("\"abc", "文字列の引用符が閉じていません")]
    [InlineData("\"a\" \"b\"", "文字列の後に不正な記述があります: \"b\"")]
    public void Syntax_IsError_WithLibraryMessage(string text, string expectedDetail)
    {
        var result = LfcValueNotation.Parse(text);

        Assert.False(result.IsSuccess);
        Assert.Equal(ValueNotationError.Syntax, result.Error);
        Assert.Equal(expectedDetail, result.ErrorDetail);
        Assert.Empty(result.Values);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData("1 # コメント")]
    [InlineData("\"a\" # コメント")]
    [InlineData("#")]
    [InlineData("1,2 #")]
    public void CommentOutsideQuotes_IsError(string text)
    {
        // # より後ろが黙って捨てられないよう、入力エラーにする
        var result = LfcValueNotation.Parse(text);

        Assert.False(result.IsSuccess);
        Assert.Equal(ValueNotationError.Comment, result.Error);
        Assert.Empty(result.Values);
    }

    [Fact]
    public void Null_Throws()
    {
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => LfcValueNotation.Parse(null!)).ParamName);
    }

    // ---- Format との往復 ----

    public static TheoryData<LfcValue[]> RoundTripCases => new()
    {
        { new LfcValue[] { 1, "tasiro", 2 } },
        { new LfcValue[] { LfcValue.Null } },
        { new LfcValue[] { "", LfcValue.Null, "x" } },
        { new LfcValue[] { 1.0, -0.5, 1e20, 0.1 } },
        { new LfcValue[] { "a\"b\\c\n\t#,=[]" } },
        { new LfcValue[] { true, "true", 1, "1" } },
    };

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Format_ThenParse_RestoresSameValues_WithoutWarnings(LfcValue[] values)
    {
        // 値の編集欄に表示した表記をそのまま確定しても、値は変わらない
        var result = LfcValueNotation.Parse(LfcValueNotation.Format(values));

        Assert.True(result.IsSuccess);
        Assert.False(result.HasWarnings);
        Assert.Equal(values, result.Values);
    }
}
