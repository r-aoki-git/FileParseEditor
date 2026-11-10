using FileParseEditor.Core.ViewModels;
using FileParserLib;

namespace FileParseEditor.Tests.ViewModels;

/// <summary>一覧の1行（SectionListItem・KeyListItem・DiagnosticListItem）のテスト（基本設計書 3-4）。</summary>
public class ListItemTests
{
    // ---- SectionListItem ----

    [Fact]
    public void Section_Global_IsShownAsGlobal()
    {
        var item = SectionListItem.From(LfcDocument.GlobalSectionName);

        Assert.Equal(new SectionListItem("", "（グローバル）"), item);
        Assert.True(item.IsGlobal);
    }

    [Theory]
    [InlineData("Profile", "[Profile]")]
    [InlineData("田代 蓮", "[田代 蓮]")]
    [InlineData("（グローバル）", "[（グローバル）]")]   // 同じ文字のセクションも、角かっこでグローバルと区別できる
    public void Section_Normal_IsShownWithBrackets(string name, string expected)
    {
        var item = SectionListItem.From(name);

        Assert.Equal(expected, item.DisplayName);
        Assert.Equal(name, item.Name);
        Assert.False(item.IsGlobal);
    }

    [Fact]
    public void Section_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SectionListItem.From(null!));
    }

    // ---- KeyListItem ----

    [Fact]
    public void Key_SingleValue()
    {
        Assert.Equal(new KeyListItem("Age", "30", "Int"), KeyListItem.From("Age", new LfcValue[] { 30 }));
    }

    [Fact]
    public void Key_MixedValues_TypesAreListedInOrder()
    {
        var item = KeyListItem.From("ID", new LfcValue[] { 1, "a", 1.5, true, LfcValue.Null });

        Assert.Equal("1,\"a\",1.5,true,", item.ValueText);
        Assert.Equal("Int, String, Double, Bool, Null", item.TypeText);
    }

    [Fact]
    public void Key_EmptyValue_IsEmptyTextAndNullType()
    {
        Assert.Equal(new KeyListItem("Memo", "", "Null"), KeyListItem.From("Memo", new LfcValue[] { LfcValue.Null }));
    }

    [Fact]
    public void Key_NullArguments_Throw()
    {
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => KeyListItem.From(null!, new LfcValue[] { 1 })).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => KeyListItem.From("K", null!)).ParamName);
    }

    // ---- DiagnosticListItem ----

    [Theory]
    [InlineData(LfcDiagnosticSeverity.Warning, "警告")]
    [InlineData(LfcDiagnosticSeverity.Error, "エラー")]
    public void Diagnostic_SeverityIsShownInJapanese(LfcDiagnosticSeverity severity, string expected)
    {
        var item = DiagnosticListItem.From(new LfcDiagnostic(severity, 7, "メッセージ"));

        Assert.Equal(new DiagnosticListItem(expected, 7, "メッセージ"), item);
    }

    [Fact]
    public void Diagnostic_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DiagnosticListItem.From(null!));
    }
}
