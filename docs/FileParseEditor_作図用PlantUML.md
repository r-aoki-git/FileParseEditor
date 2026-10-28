# FileParseEditor 作図用PlantUML

基本設計書（`FileParseEditor_基本設計書.md`）の内容を作図するためのPlantUMLソースを以下に記載する。基本設計書の本文は図がなくても読めるように記述しており、本ファイルは作図・確認用の補助資料である。astahのPlantUMLインポート機能が利用できる環境であれば、クラス図・シーケンス図はそのまま取り込める。利用できない場合は、この記述および基本設計書の本文を参照しながら作図する。

画面のレイアウト（1・2）はPlantUMLの`salt`（画面の下書き）で記述している。astahには取り込めないため、画像として出力して参照する。

## 1. メイン画面のレイアウト（基本設計書 3-2）

```plantuml
@startsalt FileParseEditor_MainWindow
{+
  FileParseEditor - sample.lfc *
  {* ファイル(F) | 編集(E) }
  {[新規] | [開く] | [保存] | . | [元に戻す] | . | [セクション追加] | [セクション削除] | . | [キー追加] | [キー削除]}
  {+
    {
      <b>セクション一覧
      {SI
        （グローバル）
        ~[Profile~]
        ~[Section~]
        .
        .
      }
    } | {
      <b>キー一覧
      {#
        <b>キー | <b>値 | <b>型
        Name | ~"田代~" | String
        ID | 1,~"tasiro~",2,~"tasrri~" | Int, String, Int, String
        Age | 30 | Int
      }
      <b>値の編集：Age
      { "30                    " | [確定] }
    } | {
      <b>保存イメージ
      {SI
        ~# プロフィール
        ~[Profile~] ~# セクションの行末コメント
        Name = ~"田代~"
        ~# 型の混在
        ID = 1,~"tasiro~",2,~"tasrri~"
        Age = 30
      }
    }
  }
  <b>診断情報
  {#
    <b>重大度 | <b>行 | <b>メッセージ
    警告 | 12 | クォートなしの文字列です: Auto
  }
  {C:\data\sample.lfc　セクション 3件　警告 1件　エラー 0件}
}
@endsalt
```

## 2. ダイアログのレイアウト（基本設計書 3-5）

```plantuml
@startsalt FileParseEditor_SectionNameDialog
{+
  セクションの追加
  .
  セクション名 | "NewSection          "
  .
  [  OK  ] | [キャンセル]
}
@endsalt
```

```plantuml
@startsalt FileParseEditor_AddKeyDialog
{+
  キーの追加
  .
  セクション | ~[Profile~]
  キー名 | "Email               "
  値 | "~"a@example.com~"     "
  .
  [  OK  ] | [キャンセル]
}
@endsalt
```

## 3. 画面遷移図（基本設計書 3-6）

```plantuml
@startuml FileParseEditor_ScreenTransition
title 画面遷移

[*] --> メイン画面 : 起動（空の文書）

state "メイン画面" as メイン画面
state "保存していない変更の確認（M01）" as 変更の確認
state "ファイルを開くダイアログ" as 開く
state "ファイルを保存するダイアログ" as 保存
state "セクションの追加ダイアログ" as セクション追加
state "キーの追加ダイアログ" as キー追加
state "セクションの削除の確認（M04）" as 削除の確認

メイン画面 --> 変更の確認 : 開く／新規作成／終了\n（変更ありの場合）
変更の確認 --> メイン画面 : キャンセル
変更の確認 --> 開く : はい・いいえ（開く）
変更の確認 --> メイン画面 : はい・いいえ（新規作成）
変更の確認 --> [*] : はい・いいえ（終了）
メイン画面 --> 開く : 開く（変更なしの場合）
開く --> メイン画面 : 選択／キャンセル
メイン画面 --> 保存 : 名前を付けて保存\n上書き保存（無題の場合）
保存 --> メイン画面 : 保存／キャンセル
メイン画面 --> セクション追加 : セクションの追加
セクション追加 --> メイン画面 : OK／キャンセル
メイン画面 --> キー追加 : キーの追加
キー追加 --> メイン画面 : OK／キャンセル
キー追加 --> キー追加 : 入力に問題あり\n（メッセージの後に再表示）
メイン画面 --> 削除の確認 : セクションの削除
削除の確認 --> メイン画面 : はい／いいえ
メイン画面 --> [*] : 終了（変更なしの場合）

@enduml
```

## 4. クラス図（基本設計書 7-3・7-4）

```plantuml
@startuml FileParseEditor_ClassDiagram
skinparam classAttributeIconSize 0
title FileParseEditor クラス図

package "FileParseEditor（WPF）" {
    package "Views" {
        class MainWindow
        class SectionNameDialog
        class AddKeyDialog
    }
    package "Services " as WpfServices {
        class FileDialogService
        class MessageService
        class InputDialogService
    }
    class App
}

package "FileParseEditor.Core" {
    package "Mvvm" {
        abstract class ObservableObject {
            +PropertyChanged : PropertyChangedEventHandler
            #SetProperty(field : T, value : T, propertyName : string) : bool
            #OnPropertyChanged(propertyName : string) : void
        }
        class RelayCommand {
            +RelayCommand(execute : Action, canExecute : Func<bool>)
            +CanExecute(parameter : object) : bool
            +Execute(parameter : object) : void
            +RaiseCanExecuteChanged() : void
        }
    }

    package "ViewModels" {
        class MainViewModel {
            +Title : string
            +StatusText : string
            +Sections : ObservableCollection<SectionListItem>
            +SelectedSection : SectionListItem
            +Keys : ObservableCollection<KeyListItem>
            +SelectedKey : KeyListItem
            +EditingValueText : string
            +PreviewText : string
            +Diagnostics : ObservableCollection<DiagnosticListItem>
            +NewCommand : ICommand
            +OpenCommand : ICommand
            +SaveCommand : ICommand
            +SaveAsCommand : ICommand
            +UndoCommand : ICommand
            +AddSectionCommand : ICommand
            +RemoveSectionCommand : ICommand
            +AddKeyCommand : ICommand
            +RemoveKeyCommand : ICommand
            +ApplyValueCommand : ICommand
            --
            +OpenFile(path : string) : void
            +ConfirmClose() : bool
        }
        class SectionListItem <<record>> {
            +Name : string
            +DisplayName : string
        }
        class KeyListItem <<record>> {
            +Key : string
            +ValueText : string
            +TypeText : string
        }
        class DiagnosticListItem <<record>> {
            +SeverityText : string
            +Line : int
            +Message : string
        }
    }

    package "Models" {
        class DocumentSession {
            +Document : LfcDocument
            +FilePath : string
            +LoadDiagnostics : IReadOnlyList<LfcDiagnostic>
            +CurrentText : string
            +IsModified : bool
            +CanUndo : bool
            --
            +Open(path : string) : void
            +New() : void
            +Save(path : string) : void
            +NeedsErrorLossConfirmation(path : string) : bool
            +SetValue(section : string, key : string, values : IReadOnlyList<LfcValue>) : void
            +AddKey(section : string, key : string, values : IReadOnlyList<LfcValue>) : void
            +RemoveKey(section : string, key : string) : void
            +AddSection(section : string) : void
            +RemoveSection(section : string) : void
            +Undo() : void
        }
        class EditHistory {
            +MaxCount : int = 100
            +CanUndo : bool
            --
            +Push(text : string) : void
            +Pop() : string
            +Clear() : void
        }
        class LfcValueNotation <<static>> {
            +{static} Format(values : IReadOnlyList<LfcValue>) : string
            +{static} Parse(text : string) : ValueNotationResult
        }
        class ValueNotationResult <<record>> {
            +Values : IReadOnlyList<LfcValue>
            +Warnings : IReadOnlyList<string>
            +ErrorMessage : string
            +IsSuccess : bool
            +HasWarnings : bool
        }
    }

    package "Services" {
        interface IFileDialogService {
            +ShowOpenDialog() : string
            +ShowSaveDialog(initialPath : string) : string
        }
        interface IMessageService {
            +ShowError(message : string) : void
            +ShowInformation(message : string) : void
            +Confirm(message : string) : bool
            +AskSaveChanges(fileName : string) : SaveChangesChoice
        }
        interface IInputDialogService {
            +AskSectionName(initialName : string) : string
            +AskNewKey(sectionDisplayName : string, initialInput : NewKeyInput) : NewKeyInput
        }
        enum SaveChangesChoice {
            Save
            Discard
            Cancel
        }
        class NewKeyInput <<record>> {
            +Key : string
            +ValueText : string
        }
    }
}

package "FileParserLib（lib/FileParserLib.dll）" {
    class LfcDocument
    class LfcValue <<struct>>
    class LfcDiagnostic <<record>>
}

ObservableObject <|-- MainViewModel
MainViewModel o-- "1" DocumentSession
MainViewModel o-- "1..*" RelayCommand
MainViewModel o-- "0..*" SectionListItem
MainViewModel o-- "0..*" KeyListItem
MainViewModel o-- "0..*" DiagnosticListItem
MainViewModel --> "1" IFileDialogService
MainViewModel --> "1" IMessageService
MainViewModel --> "1" IInputDialogService
MainViewModel ..> LfcValueNotation : uses

DocumentSession o-- "1" LfcDocument
DocumentSession o-- "1" EditHistory
DocumentSession o-- "0..*" LfcDiagnostic
DocumentSession ..> LfcValueNotation : uses
LfcValueNotation ..> LfcDocument : uses
LfcValueNotation ..> ValueNotationResult : creates
ValueNotationResult o-- "0..*" LfcValue

IMessageService ..> SaveChangesChoice
IInputDialogService ..> NewKeyInput

FileDialogService ..|> IFileDialogService
MessageService ..|> IMessageService
InputDialogService ..|> IInputDialogService
InputDialogService ..> SectionNameDialog : shows
InputDialogService ..> AddKeyDialog : shows
MainWindow ..> MainViewModel : DataContext
App ..> MainWindow : creates
App ..> MainViewModel : creates

@enduml
```

## 5. シーケンス図（ファイルを開く）（基本設計書 8-1）

```plantuml
@startuml FileParseEditor_Sequence_Open
title ファイルを開く

actor 利用者 as User
participant "MainWindow" as View
participant "MainViewModel" as VM
participant "IMessageService" as Msg
participant "IFileDialogService" as Dlg
participant "DocumentSession" as Session
participant "LfcDocument\n(FileParserLib)" as Doc

User -> View : メニュー「開く」
View -> VM : OpenCommand.Execute()
activate VM

VM -> Session : IsModified
Session --> VM : true
VM -> Msg : AskSaveChanges(ファイル名)
Msg --> VM : Discard（いいえ）

VM -> Dlg : ShowOpenDialog()
Dlg --> VM : path

VM -> Session : Open(path)
activate Session
Session -> Doc : Load(path, ThrowOnError = false)
activate Doc
alt 読み込みに成功
    Doc --> Session : LfcDocument（Diagnostics を含む）
    deactivate Doc
    Session -> Doc : ToText()
    Doc --> Session : テキスト
    note right of Session
      文書・パス・診断情報・
      保存済みの内容を置き換え、
      元に戻す履歴を消去する
    end note
    Session --> VM
    VM -> VM : 表示を更新\n（一覧・保存イメージ・診断情報・タイトル）
    opt 診断情報に Error がある
        VM -> Msg : ShowInformation(M02)
    end
else 例外（FileNotFoundException 等）
    Doc --> Session : 例外
    Session --> VM : 例外（状態は変えない）
    VM -> Msg : ShowError(M11)
end
deactivate Session

deactivate VM

@enduml
```

## 6. シーケンス図（値の変更）（基本設計書 8-2）

```plantuml
@startuml FileParseEditor_Sequence_ApplyValue
title 値の変更

actor 利用者 as User
participant "MainWindow" as View
participant "MainViewModel" as VM
participant "LfcValueNotation" as Notation
participant "IMessageService" as Msg
participant "DocumentSession" as Session
participant "EditHistory" as History
participant "LfcDocument\n(FileParserLib)" as Doc

User -> View : 値を入力して「確定」
View -> VM : ApplyValueCommand.Execute()
activate VM

VM -> Notation : Parse(EditingValueText)
activate Notation
Notation -> Doc : Parse("K = " + 入力, ThrowOnError = false)
Doc --> Notation : 作業用の文書（Diagnostics を含む）
note right of Notation
  改行・Error・コメントを検査し、
  値と Warning を取り出す（5-2）
end note
Notation --> VM : ValueNotationResult
deactivate Notation

alt 入力エラー
    VM -> Msg : ShowError(M06〜M08)
else 成功・警告付き
    opt 警告付き
        VM -> Msg : Confirm(M05)
        Msg --> VM : true（はい。「いいえ」なら終了）
    end
    VM -> Session : SetValue(section, key, values)
    activate Session
    Session -> Doc : ToText()
    Doc --> Session : 変更前のテキスト
    Session -> History : Push(変更前のテキスト)
    Session -> Doc : Set(section, key, values)
    Session -> Doc : ToText()
    Doc --> Session : 変更後のテキスト
    Session --> VM
    deactivate Session
    VM -> VM : キー一覧・保存イメージ・タイトルを更新
end

deactivate VM

@enduml
```

## 7. シーケンス図（元に戻す）（基本設計書 8-3）

```plantuml
@startuml FileParseEditor_Sequence_Undo
title 元に戻す

actor 利用者 as User
participant "MainWindow" as View
participant "MainViewModel" as VM
participant "DocumentSession" as Session
participant "EditHistory" as History
participant "LfcDocument\n(FileParserLib)" as Doc

User -> View : メニュー「元に戻す」
View -> VM : UndoCommand.Execute()
activate VM

VM -> VM : 選択中のセクション・キーを覚える
VM -> Session : Undo()
activate Session
Session -> History : Pop()
History --> Session : 変更前のテキスト
Session -> Doc : Parse(変更前のテキスト, ThrowOnError = false)
Doc --> Session : 作り直した LfcDocument
note right of Session
  現在の文書と置き換える。
  診断情報はファイルを開いた
  時点のものを残す
end note
Session --> VM
deactivate Session

VM -> VM : 表示を更新し、覚えたセクション・キーが\nあれば選択し直す

deactivate VM

@enduml
```

## 8. シーケンス図（上書き保存：エラーを含むファイル）（基本設計書 8-4）

```plantuml
@startuml FileParseEditor_Sequence_Save
title 上書き保存（エラーを含むファイル）

actor 利用者 as User
participant "MainWindow" as View
participant "MainViewModel" as VM
participant "IMessageService" as Msg
participant "DocumentSession" as Session
participant "LfcDocument\n(FileParserLib)" as Doc

User -> View : メニュー「上書き保存」
View -> VM : SaveCommand.Execute()
activate VM

VM -> Session : FilePath
Session --> VM : path（開いたファイル）

VM -> Session : NeedsErrorLossConfirmation(path)
Session --> VM : true
VM -> Msg : Confirm(M03)
Msg --> VM : true（はい）

VM -> Session : Save(path)
activate Session
Session -> Doc : Save(path)
alt 保存に成功
    note right of Doc
      UTF-8（BOMなし）・LF で書き込む
    end note
    Session -> Doc : ToText()
    Doc --> Session : 保存した内容
    note right of Session
      保存済みの内容として記録する。
      以降、M03 の確認は行わない
    end note
    Session --> VM
    VM -> VM : タイトルを更新（「 *」を外す）
else 例外（UnauthorizedAccessException 等）
    Doc --> Session : 例外
    Session --> VM : 例外（状態は変えない）
    VM -> Msg : ShowError(M12)
end
deactivate Session

deactivate VM

@enduml
```

## 9. アクティビティ図（値の入力の解釈）（基本設計書 5-2）

```plantuml
@startuml FileParseEditor_Activity_ParseValue
title 値の入力の解釈（LfcValueNotation.Parse）

start
:入力された表記;
if (改行（CR・LF）を含む？) then (はい)
    :入力エラー（M07）;
    stop
endif
:LfcDocument.Parse("K = " + 入力, ThrowOnError = false);
if (診断情報に Error がある？) then (はい)
    :入力エラー（M06）;
    stop
endif
:解釈した値を取り出す;
if (文書の ToText() と、値だけを書き出した結果が異なる？) then (はい)
    :入力エラー（M08）\nクォートの外の # がコメントとして扱われた;
    stop
endif
if (診断情報に Warning がある？) then (はい)
    :警告付きで成功\n（値と Warning のメッセージを返す）;
else (いいえ)
    :成功（値を返す）;
endif
stop

@enduml
```

## 10. アクティビティ図（保存していない変更の確認）（基本設計書 6-10）

```plantuml
@startuml FileParseEditor_Activity_ConfirmChanges
title 保存していない変更の確認（開く・新規作成・終了の前）

start
if (変更あり？\n（現在の ToText() ≠ 保存済みの内容）) then (いいえ)
    :元の操作を続ける;
    stop
endif
:M01「変更を保存しますか？」;
switch (選択)
case (はい)
    :上書き保存（基本設計書 6-8）;
    if (保存できた？) then (はい)
        :元の操作を続ける;
    else (キャンセル・失敗)
        :元の操作を中止する;
    endif
case (いいえ)
    :保存せずに元の操作を続ける;
case (キャンセル)
    :元の操作を中止する;
endswitch
stop

@enduml
```
