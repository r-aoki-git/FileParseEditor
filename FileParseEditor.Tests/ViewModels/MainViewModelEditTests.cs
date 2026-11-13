using FileParseEditor.Core.Services;
using FileParseEditor.Core.ViewModels;
using FileParseEditor.Tests.Helpers;

namespace FileParseEditor.Tests.ViewModels;

/// <summary>MainViewModel：編集・元に戻す・コマンドの実行できる条件のテスト（基本設計書 3-3、3-4-3、6-3〜6-7、6-9）。</summary>
public class MainViewModelEditTests : IDisposable
{
    private const string Content = "[Profile]\n# 名前\nName = \"田代\"\nAge = 30\nMemo =\n\n[Section]\nList = 10,20\n";

    private readonly TemporaryDirectory _dir = new();
    private readonly FakeFileDialogService _fileDialog = new();
    private readonly FakeMessageService _messages = new();
    private readonly FakeInputDialogService _inputDialog = new();
    private readonly MainViewModel _viewModel;
    private readonly string _openedPreview;

    public MainViewModelEditTests()
    {
        _viewModel = new MainViewModel(_fileDialog, _messages, _inputDialog);
        _viewModel.OpenDroppedFiles(new[] { _dir.WriteText("a.lfc", Content) });
        _openedPreview = _viewModel.PreviewText;
    }

    public void Dispose() => _dir.Dispose();

    // ---- コマンドの実行できる条件（基本設計書 3-3） ----

    [Fact]
    public void Commands_AfterOpen_GlobalSectionNotInFileSelected()
    {
        Assert.False(_viewModel.UndoCommand.CanExecute(null));
        Assert.True(_viewModel.AddSectionCommand.CanExecute(null));
        Assert.False(_viewModel.RemoveSectionCommand.CanExecute(null));   // グローバルセクションはファイルにない
        Assert.True(_viewModel.AddKeyCommand.CanExecute(null));           // グローバルセクションにも追加できる
        Assert.False(_viewModel.RemoveKeyCommand.CanExecute(null));
        Assert.False(_viewModel.ApplyValueCommand.CanExecute(null));
        Assert.False(_viewModel.IsKeySelected);
    }

    [Fact]
    public void Commands_FollowSelection()
    {
        SelectSection("Profile");
        Assert.True(_viewModel.RemoveSectionCommand.CanExecute(null));
        Assert.False(_viewModel.RemoveKeyCommand.CanExecute(null));

        SelectKey("Name");
        Assert.True(_viewModel.RemoveKeyCommand.CanExecute(null));
        Assert.True(_viewModel.ApplyValueCommand.CanExecute(null));
        Assert.True(_viewModel.IsKeySelected);

        _viewModel.SelectedKey = null;
        Assert.False(_viewModel.RemoveKeyCommand.CanExecute(null));
        Assert.False(_viewModel.IsKeySelected);
    }

    [Fact]
    public void Commands_NotifyWhenSelectionChanges()
    {
        var notified = 0;
        _viewModel.RemoveKeyCommand.CanExecuteChanged += (_, _) => notified++;
        SelectSection("Profile");
        notified = 0;

        SelectKey("Name");

        Assert.True(notified > 0);
    }

    [Fact]
    public void Commands_NotExecutable_DoNothing()
    {
        // ショートカットキーなどで呼ばれても、実行できない状態では何もしない
        _viewModel.RemoveKeyCommand.Execute(null);
        _viewModel.ApplyValueCommand.Execute(null);
        _viewModel.UndoCommand.Execute(null);
        _viewModel.RemoveSectionCommand.Execute(null);

        AssertUnchanged();
        Assert.Empty(_messages.Errors);
        Assert.Empty(_messages.Confirmations);
    }

    // ---- 値の編集欄（基本設計書 3-4-3） ----

    [Fact]
    public void SelectKey_ShowsItsNotationInEditor()
    {
        SelectSection("Profile");

        SelectKey("Name");
        Assert.Equal("\"田代\"", _viewModel.EditingValueText);

        SelectKey("Memo");
        Assert.Equal("", _viewModel.EditingValueText);
    }

    [Fact]
    public void SelectAnotherKey_DiscardsUnappliedInput()
    {
        SelectSection("Profile");
        SelectKey("Name");
        _viewModel.EditingValueText = "\"入力中\"";

        SelectKey("Age");

        Assert.Equal("30", _viewModel.EditingValueText);
        AssertUnchanged();
    }

    [Fact]
    public void ChangeSection_ClearsEditor()
    {
        SelectSection("Profile");
        SelectKey("Name");

        SelectSection("Section");

        Assert.Equal("", _viewModel.EditingValueText);
        Assert.False(_viewModel.IsKeySelected);
    }

    // ---- 値の変更（基本設計書 6-3） ----

    [Fact]
    public void ApplyValue_ChangesValue_KeepsSelectionAndPosition()
    {
        SelectSection("Profile");
        SelectKey("Name");
        _viewModel.EditingValueText = "\"青木\", 1";

        _viewModel.ApplyValueCommand.Execute(null);

        Assert.Equal(new[] { "Name", "Age", "Memo" }, _viewModel.Keys.Select(k => k.Key));
        Assert.Equal(new KeyListItem("Name", "\"青木\",1", "String, Int"), _viewModel.SelectedKey);
        Assert.Equal("\"青木\",1", _viewModel.EditingValueText);   // 正規化後の表記
        Assert.Contains("# 名前\nName = \"青木\",1\n", _viewModel.PreviewText);
        Assert.Equal("FileParseEditor - a.lfc *", _viewModel.Title);
        Assert.True(_viewModel.UndoCommand.CanExecute(null));
        Assert.Empty(_messages.Errors);
    }

    [Fact]
    public void ApplyValue_EmptyInput_SetsNoValue()
    {
        SelectSection("Profile");
        SelectKey("Age");
        _viewModel.EditingValueText = "";

        _viewModel.ApplyValueCommand.Execute(null);

        Assert.Equal(new KeyListItem("Age", "", "Null"), _viewModel.SelectedKey);
        Assert.Contains("Age =\n", _viewModel.PreviewText);
    }

    [Fact]
    public void ApplyValue_SameValues_IsNotAChange_ButNotationIsNormalized()
    {
        SelectSection("Profile");
        SelectKey("Age");
        _viewModel.EditingValueText = "030";   // Int(30) と同じ値

        _viewModel.ApplyValueCommand.Execute(null);

        Assert.Equal("30", _viewModel.EditingValueText);
        Assert.False(_viewModel.UndoCommand.CanExecute(null));   // 元に戻す履歴にも残さない
        AssertUnchanged();
    }

    [Fact]
    public void ApplyValue_Warning_Yes_IsApplied()
    {
        SelectSection("Profile");
        SelectKey("Age");
        _viewModel.EditingValueText = "Auto";
        _messages.ConfirmAnswers.Enqueue(true);

        _viewModel.ApplyValueCommand.Execute(null);

        Assert.Equal(
            new[] { "入力した値には次の注意点があります。\nクォートなしの文字列です: Auto\n\n次のように解釈します。\n\"Auto\"\n\nこのまま確定しますか？" },
            _messages.Confirmations);
        Assert.Equal(new KeyListItem("Age", "\"Auto\"", "String"), _viewModel.SelectedKey);
        Assert.Equal("\"Auto\"", _viewModel.EditingValueText);
    }

    [Fact]
    public void ApplyValue_Warning_No_ChangesNothing_AndKeepsInput()
    {
        SelectSection("Profile");
        SelectKey("Age");
        _viewModel.EditingValueText = "Auto";
        _messages.ConfirmAnswers.Enqueue(false);

        _viewModel.ApplyValueCommand.Execute(null);

        Assert.Single(_messages.Confirmations);
        Assert.Equal("Auto", _viewModel.EditingValueText);
        AssertUnchanged();
    }

    [Theory]
    [InlineData("\"abc", "入力した値を解釈できません。\n文字列の引用符が閉じていません")]
    [InlineData("1\n2", "値には改行を入力できません。文字列の中で改行する場合は \\n と入力してください。")]
    [InlineData("1 # c", "値にはコメント（#）を入力できません。# を文字の一部として使う場合は、\" \" で囲んで文字列にしてください。")]
    public void ApplyValue_InputError_ShowsError_ChangesNothing_AndKeepsInput(string input, string expectedMessage)
    {
        SelectSection("Profile");
        SelectKey("Age");
        _viewModel.EditingValueText = input;

        _viewModel.ApplyValueCommand.Execute(null);

        Assert.Equal(new[] { expectedMessage }, _messages.Errors);
        Assert.Empty(_messages.Confirmations);
        Assert.Equal(input, _viewModel.EditingValueText);
        AssertUnchanged();
    }

    // ---- キーの追加（基本設計書 6-4） ----

    [Fact]
    public void AddKey_AppendsAndSelectsIt()
    {
        SelectSection("Profile");
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("Email", "\"a@example.com\""));

        _viewModel.AddKeyCommand.Execute(null);

        Assert.Equal(new[] { ("[Profile]", NewKeyInput.Empty) }, _inputDialog.NewKeyRequests);
        Assert.Equal(new[] { "Name", "Age", "Memo", "Email" }, _viewModel.Keys.Select(k => k.Key));
        Assert.Equal(new KeyListItem("Email", "\"a@example.com\"", "String"), _viewModel.SelectedKey);
        Assert.Equal("\"a@example.com\"", _viewModel.EditingValueText);
        Assert.Contains("Email = \"a@example.com\"\n\n[Section]", _viewModel.PreviewText);
        Assert.EndsWith(" *", _viewModel.Title);
    }

    [Fact]
    public void AddKey_Cancel_ChangesNothing()
    {
        SelectSection("Profile");

        _viewModel.AddKeyCommand.Execute(null);   // 答えを積んでいないため、キャンセルになる

        Assert.Single(_inputDialog.NewKeyRequests);
        AssertUnchanged();
    }

    [Fact]
    public void AddKey_ToGlobalSectionNotInFile_CreatesIt()
    {
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("Version", "1"));

        _viewModel.AddKeyCommand.Execute(null);

        Assert.Equal("（グローバル）", _inputDialog.NewKeyRequests[0].Section);
        Assert.Equal(new[] { new KeyListItem("Version", "1", "Int") }, _viewModel.Keys);
        Assert.StartsWith("Version = 1\n", _viewModel.PreviewText);
        Assert.Contains("セクション 3件", _viewModel.StatusText);
        Assert.True(_viewModel.RemoveSectionCommand.CanExecute(null));   // グローバルセクションが文書にできた
    }

    [Fact]
    public void AddKey_ExistingKey_ShowsError_AndAsksAgainWithSameInput()
    {
        SelectSection("Profile");
        var input = new NewKeyInput("Name", "\"青木\"");
        _inputDialog.NewKeyAnswers.Enqueue(input);

        _viewModel.AddKeyCommand.Execute(null);

        Assert.Equal(new[] { "キー Name は既に存在します。" }, _messages.Errors);
        Assert.Equal(input, _inputDialog.NewKeyRequests[1].Input);   // 入力を残したまま再表示する
        AssertUnchanged();                                             // 既存の値を上書きしない
    }

    [Fact]
    public void AddKey_EmptyName_ShowsError_AndAsksAgain()
    {
        SelectSection("Profile");
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("", "1"));

        _viewModel.AddKeyCommand.Execute(null);

        Assert.Equal(new[] { "名前を入力してください。" }, _messages.Errors);
        Assert.Equal(2, _inputDialog.NewKeyRequests.Count);
        AssertUnchanged();
    }

    [Theory]
    [InlineData("a=b", "この名前は使用できません。\nキー名に '=' は使用できません: a=b")]
    [InlineData("[a", "この名前は使用できません。\nキー名を '[' で始めることはできません: [a")]
    [InlineData(" a", "この名前は使用できません。\nキー名の前後に空白は使用できません: ' a'")]
    public void AddKey_InvalidName_ShowsLibraryReason_AndAsksAgain(string key, string expectedMessage)
    {
        SelectSection("Profile");
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput(key, "1"));

        _viewModel.AddKeyCommand.Execute(null);

        // .NET が付ける「 (Parameter 'key')」は表示しない
        Assert.Equal(new[] { expectedMessage }, _messages.Errors);
        Assert.Equal(2, _inputDialog.NewKeyRequests.Count);
        Assert.False(_viewModel.UndoCommand.CanExecute(null));   // 失敗した操作は履歴に残らない
        AssertUnchanged();
    }

    [Fact]
    public void AddKey_ValueError_ShowsError_AndAsksAgainWithSameInput()
    {
        SelectSection("Profile");
        var input = new NewKeyInput("Email", "\"abc");
        _inputDialog.NewKeyAnswers.Enqueue(input);

        _viewModel.AddKeyCommand.Execute(null);

        Assert.Equal(new[] { "入力した値を解釈できません。\n文字列の引用符が閉じていません" }, _messages.Errors);
        Assert.Equal(input, _inputDialog.NewKeyRequests[1].Input);
        AssertUnchanged();
    }

    [Fact]
    public void AddKey_ValueWarning_No_AsksAgain_ThenFixedInputIsAdded()
    {
        SelectSection("Profile");
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("Mode", "Auto"));
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("Mode", "\"Manual\""));
        _messages.ConfirmAnswers.Enqueue(false);

        _viewModel.AddKeyCommand.Execute(null);

        Assert.Single(_messages.Confirmations);
        Assert.Equal(new NewKeyInput("Mode", "Auto"), _inputDialog.NewKeyRequests[1].Input);
        Assert.Equal(new KeyListItem("Mode", "\"Manual\"", "String"), _viewModel.SelectedKey);
    }

    [Fact]
    public void AddKey_ValueWarning_Yes_IsAdded()
    {
        SelectSection("Profile");
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("Mode", "Auto"));
        _messages.ConfirmAnswers.Enqueue(true);

        _viewModel.AddKeyCommand.Execute(null);

        Assert.Equal(new KeyListItem("Mode", "\"Auto\"", "String"), _viewModel.SelectedKey);
        Assert.Single(_inputDialog.NewKeyRequests);
    }

    // ---- キーの削除（基本設計書 6-5） ----

    [Fact]
    public void RemoveKey_WithoutConfirmation_SelectsNextKey()
    {
        SelectSection("Profile");
        SelectKey("Name");

        _viewModel.RemoveKeyCommand.Execute(null);

        Assert.Empty(_messages.Confirmations);
        Assert.Equal(new[] { "Age", "Memo" }, _viewModel.Keys.Select(k => k.Key));
        Assert.Equal("Age", _viewModel.SelectedKey?.Key);
        Assert.Equal("30", _viewModel.EditingValueText);
        Assert.DoesNotContain("# 名前", _viewModel.PreviewText);   // 付いていたコメントも削除される
        Assert.EndsWith(" *", _viewModel.Title);
    }

    [Fact]
    public void RemoveKey_Last_SelectsPreviousKey()
    {
        SelectSection("Profile");
        SelectKey("Memo");

        _viewModel.RemoveKeyCommand.Execute(null);

        Assert.Equal("Age", _viewModel.SelectedKey?.Key);
    }

    [Fact]
    public void RemoveKey_Only_SelectsNothing()
    {
        SelectSection("Section");
        SelectKey("List");

        _viewModel.RemoveKeyCommand.Execute(null);

        Assert.Empty(_viewModel.Keys);
        Assert.Null(_viewModel.SelectedKey);
        Assert.False(_viewModel.RemoveKeyCommand.CanExecute(null));
        Assert.True(_viewModel.RemoveSectionCommand.CanExecute(null));   // キーがなくてもセクションは残る
    }

    // ---- セクションの追加（基本設計書 6-6） ----

    [Fact]
    public void AddSection_AppendsAndSelectsIt()
    {
        _inputDialog.SectionNameAnswers.Enqueue("New");

        _viewModel.AddSectionCommand.Execute(null);

        Assert.Equal(new[] { "" }, _inputDialog.SectionNameInitialValues);
        Assert.Equal(new[] { "（グローバル）", "[Profile]", "[Section]", "[New]" }, _viewModel.Sections.Select(s => s.DisplayName));
        Assert.Equal("New", _viewModel.SelectedSection?.Name);
        Assert.Empty(_viewModel.Keys);
        Assert.EndsWith("[New]\n", _viewModel.PreviewText);
        Assert.EndsWith(" *", _viewModel.Title);
    }

    [Fact]
    public void AddSection_Cancel_ChangesNothing()
    {
        _viewModel.AddSectionCommand.Execute(null);

        AssertUnchanged();
    }

    [Fact]
    public void AddSection_EmptyName_ShowsError_AndAsksAgain()
    {
        _inputDialog.SectionNameAnswers.Enqueue("");

        _viewModel.AddSectionCommand.Execute(null);

        Assert.Equal(new[] { "名前を入力してください。" }, _messages.Errors);
        Assert.Equal(2, _inputDialog.SectionNameInitialValues.Count);
        AssertUnchanged();
    }

    [Fact]
    public void AddSection_Existing_ShowsInformation_AndSelectsIt()
    {
        _inputDialog.SectionNameAnswers.Enqueue("Section");

        _viewModel.AddSectionCommand.Execute(null);

        Assert.Equal(new[] { "セクション [Section] は既に存在します。" }, _messages.Informations);
        Assert.Empty(_messages.Errors);
        Assert.Equal("Section", _viewModel.SelectedSection?.Name);
        Assert.Single(_inputDialog.SectionNameInitialValues);   // ダイアログは再表示しない
        AssertUnchanged();
    }

    [Fact]
    public void AddSection_InvalidName_ShowsLibraryReason_AndAsksAgainWithSameName()
    {
        _inputDialog.SectionNameAnswers.Enqueue("[New");

        _viewModel.AddSectionCommand.Execute(null);

        Assert.Equal(new[] { "この名前は使用できません。\nセクション名に '[' は使用できません: [New" }, _messages.Errors);
        Assert.Equal(new[] { "", "[New" }, _inputDialog.SectionNameInitialValues);
        AssertUnchanged();
    }

    // ---- セクションの削除（基本設計書 6-7） ----

    [Fact]
    public void RemoveSection_Yes_RemovesAndSelectsFirst()
    {
        SelectSection("Profile");
        _messages.ConfirmAnswers.Enqueue(true);

        _viewModel.RemoveSectionCommand.Execute(null);

        Assert.Equal(new[] { "セクション [Profile] と、含まれるキー 3 件を削除しますか？" }, _messages.Confirmations);
        Assert.Equal(new[] { "（グローバル）", "[Section]" }, _viewModel.Sections.Select(s => s.DisplayName));
        Assert.Same(_viewModel.Sections[0], _viewModel.SelectedSection);
        Assert.Equal("[Section]\nList = 10,20\n", _viewModel.PreviewText);
    }

    [Fact]
    public void RemoveSection_No_ChangesNothing()
    {
        SelectSection("Profile");
        _messages.ConfirmAnswers.Enqueue(false);

        _viewModel.RemoveSectionCommand.Execute(null);

        Assert.Equal("Profile", _viewModel.SelectedSection?.Name);
        AssertUnchanged();
    }

    [Fact]
    public void RemoveSection_Global_StaysInListAndSelected()
    {
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("Version", "1"));
        _viewModel.AddKeyCommand.Execute(null);
        _messages.ConfirmAnswers.Enqueue(true);

        _viewModel.RemoveSectionCommand.Execute(null);

        Assert.Equal("セクション （グローバル） と、含まれるキー 1 件を削除しますか？", _messages.Confirmations[0]);
        Assert.Equal("（グローバル）", _viewModel.Sections[0].DisplayName);
        Assert.Same(_viewModel.Sections[0], _viewModel.SelectedSection);
        Assert.Empty(_viewModel.Keys);
        Assert.False(_viewModel.RemoveSectionCommand.CanExecute(null));
    }

    // ---- 元に戻す（基本設計書 6-9） ----

    [Fact]
    public void Undo_RemovedKey_ComesBackWithComment_AndIsSelectedAgain()
    {
        SelectSection("Profile");
        SelectKey("Name");
        _viewModel.RemoveKeyCommand.Execute(null);

        _viewModel.UndoCommand.Execute(null);

        Assert.Equal(new[] { "Name", "Age", "Memo" }, _viewModel.Keys.Select(k => k.Key));
        Assert.Equal("Profile", _viewModel.SelectedSection?.Name);
        Assert.Equal("Age", _viewModel.SelectedKey?.Key);       // 元に戻す直前に選択していたキー
        AssertUnchanged();
    }

    [Fact]
    public void Undo_RemovedSection_ComesBack_AndFirstIsSelected()
    {
        SelectSection("Profile");
        _messages.ConfirmAnswers.Enqueue(true);
        _viewModel.RemoveSectionCommand.Execute(null);

        _viewModel.UndoCommand.Execute(null);

        Assert.Equal(new[] { "（グローバル）", "[Profile]", "[Section]" }, _viewModel.Sections.Select(s => s.DisplayName));
        Assert.Same(_viewModel.Sections[0], _viewModel.SelectedSection);   // 削除後に選択していたのはグローバル
        AssertUnchanged();
    }

    [Fact]
    public void Undo_AddedSection_RemovesIt_AndSelectsFirst()
    {
        _inputDialog.SectionNameAnswers.Enqueue("New");
        _viewModel.AddSectionCommand.Execute(null);

        _viewModel.UndoCommand.Execute(null);

        Assert.Same(_viewModel.Sections[0], _viewModel.SelectedSection);   // 選択していた [New] はなくなった
        AssertUnchanged();
    }

    [Fact]
    public void Undo_SeveralSteps_BackToOpenedFile()
    {
        SelectSection("Profile");
        SelectKey("Age");
        _viewModel.EditingValueText = "31";
        _viewModel.ApplyValueCommand.Execute(null);
        _inputDialog.NewKeyAnswers.Enqueue(new NewKeyInput("Email", "\"a\""));
        _viewModel.AddKeyCommand.Execute(null);

        _viewModel.UndoCommand.Execute(null);
        Assert.Equal(new[] { "Name", "Age", "Memo" }, _viewModel.Keys.Select(k => k.Key));
        Assert.EndsWith(" *", _viewModel.Title);

        _viewModel.UndoCommand.Execute(null);
        Assert.Equal("FileParseEditor - a.lfc", _viewModel.Title);   // 開いた時点と同じ内容に戻れば「変更あり」を解除する
        Assert.False(_viewModel.UndoCommand.CanExecute(null));
        AssertUnchanged();
    }

    [Fact]
    public void Undo_KeepsDiagnosticsOfOpenedFile()
    {
        _viewModel.OpenDroppedFiles(new[] { _dir.WriteText("errors.lfc", "[A]\nK = 1\nK = 2\nMode = Auto\n") });
        SelectSection("A");
        SelectKey("K");
        _viewModel.EditingValueText = "3";
        _viewModel.ApplyValueCommand.Execute(null);

        _viewModel.UndoCommand.Execute(null);

        Assert.Equal(new[] { ("エラー", 3), ("警告", 4) }, _viewModel.Diagnostics.Select(d => (d.SeverityText, d.Line)));
        Assert.Contains("警告 1件    エラー 1件", _viewModel.StatusText);
    }

    [Fact]
    public void Open_ClearsUndoHistory()
    {
        SelectSection("Profile");
        SelectKey("Age");
        _viewModel.EditingValueText = "31";
        _viewModel.ApplyValueCommand.Execute(null);

        _viewModel.OpenDroppedFiles(new[] { _dir.WriteText("b.lfc", "K = 1\n") });

        Assert.False(_viewModel.UndoCommand.CanExecute(null));
    }

    private void SelectSection(string name) =>
        _viewModel.SelectedSection = _viewModel.Sections.Single(s => s.Name == name);

    private void SelectKey(string key) =>
        _viewModel.SelectedKey = _viewModel.Keys.Single(k => k.Key == key);

    private void AssertUnchanged()
    {
        Assert.Equal(_openedPreview, _viewModel.PreviewText);
        Assert.Equal("FileParseEditor - a.lfc", _viewModel.Title);
    }
}
