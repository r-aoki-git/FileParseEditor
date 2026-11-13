using FileParseEditor.Core.Models;
using FileParserLib;

namespace FileParseEditor.Tests.Models;

/// <summary>LfcValueNotation：値 → 表記（Format）のテスト（基本設計書 5-1）。</summary>
public class LfcValueNotationFormatTests
{
    public static TheoryData<LfcValue[], string> FormatCases => new()
    {
        { new LfcValue[] { 1 }, "1" },
        { new LfcValue[] { 1, "tasiro", 2 }, "1,\"tasiro\",2" },     // カンマの後に空白を入れない
        { new LfcValue[] { LfcValue.Null }, "" },                     // 値なしは空文字列
        { new LfcValue[] { LfcValue.Null, LfcValue.Null }, "," },     // 空の要素2つ
        { new LfcValue[] { "x", LfcValue.Null, 2 }, "\"x\",,2" },
        { new LfcValue[] { 1.0 }, "1.0" },                            // Double は小数点を含む表記（Int と区別する）
        { new LfcValue[] { -0.5 }, "-0.5" },
        { new LfcValue[] { true, false }, "true,false" },
        { new LfcValue[] { "" }, "\"\"" },                            // 空文字列は値なしと区別する
        { new LfcValue[] { "a\"b\\c" }, "\"a\\\"b\\\\c\"" },          // " と \ はエスケープする
        { new LfcValue[] { "1行目\n2行目\tタブ" }, "\"1行目\\n2行目\\tタブ\"" },  // 改行・タブもエスケープし、1行に収める
        { new LfcValue[] { "a,b # c" }, "\"a,b # c\"" },              // , と # は文字列の中ならそのまま
        { new LfcValue[] { "123" }, "\"123\"" },                      // 数字だけの文字列も String のまま（クォートを付ける）
    };

    [Theory]
    [MemberData(nameof(FormatCases))]
    public void Format(LfcValue[] values, string expected)
    {
        Assert.Equal(expected, LfcValueNotation.Format(values));
    }

    [Theory]
    [MemberData(nameof(FormatCases))]
    public void Format_ThenParseByLibrary_RestoresSameValues(LfcValue[] values, string _)
    {
        // 表示した表記をそのまま LFC として読み込むと、同じ型・同じ値に戻る（表示が保存時の表記と一致していること）
        var notation = LfcValueNotation.Format(values);

        var document = LfcDocument.Parse("K = " + notation);

        Assert.Empty(document.Diagnostics);
        Assert.Equal(values, document.Get(LfcDocument.GlobalSectionName, "K"));
    }

    [Fact]
    public void Format_DoesNotChangeTheGivenList()
    {
        var values = new LfcValue[] { 1, "a" };

        LfcValueNotation.Format(values);

        Assert.Equal(new LfcValue[] { 1, "a" }, values);
    }

    [Fact]
    public void Format_Null_Throws()
    {
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => LfcValueNotation.Format(null!)).ParamName);
    }

    [Fact]
    public void Format_NoValues_Throws()
    {
        // ライブラリの値は必ず1件以上（値なしも Null 1件）のため、0件は不正な引数
        Assert.Throws<ArgumentException>(() => LfcValueNotation.Format(Array.Empty<LfcValue>()));
    }
}
