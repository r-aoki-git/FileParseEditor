# FileParseEditor

LFC（`.lfc`）ファイルの読み込み・編集・保存を画面から行う、Windows 用の WPF アプリケーション。
LFC の読み書きは`FileParserLib`（`lib/FileParserLib.dll`）に任せる。
仕様は`docs/FileParseEditor_要件定義書.md`と`docs/FileParseEditor_基本設計書.md`を参照。

## ソリューションの構成

1ファイルには1つの型だけを定義し、ファイル名は型名と同じにする。フォルダ名と名前空間（namespace）は一致させる（`FileParserLib`と同じ方針）。

```
FileParseEditor/            WPF アプリ（net8.0-windows）… 画面と、WPF に依存する処理
├── App.xaml(.cs)             起動時に ViewModel とサービスを組み立てる。想定外の例外を受け止める
├── Views/                    画面（MainWindow）
└── Services/                 ダイアログ・メッセージボックスの実装

FileParseEditor.Core/       クラスライブラリ（net8.0）… 画面に依存しない処理
├── Mvvm/                     MVVM の基盤（ObservableObject、RelayCommand・AsyncRelayCommand、BulkObservableCollection）
├── Models/                   文書の管理（DocumentSession）、元に戻す履歴（EditHistory）、値の表記の変換（LfcValueNotation）
├── ViewModels/               画面の表示内容とコマンド（MainViewModel）、ダイアログを伴う編集の流れ（EditDialogFlow）、
│                             保存と保存していない変更の確認の流れ（FileSaveFlow）、
│                             一覧の行とその作成（ListItemBuilder）、メッセージの文言
└── Services/                 ダイアログ・メッセージのインターフェイス

FileParseEditor.Tests/      単体テスト（xUnit、net8.0）。フォルダ構成は FileParseEditor.Core と同じにする
└── Helpers/                  テスト用の偽のサービス、一時フォルダ

lib/                        参照する FileParserLib.dll（どのコミットからビルドしたかは lib/README.md）
docs/                       要件定義書・基本設計書・スケジュール・作図用 PlantUML
diagrams/                   作図用 PlantUML を図ごとに分けたファイル
```

- ViewModel を WPF に依存しない`FileParseEditor.Core`に置き、ViewModel から画面の部品を使えないようにしている（基本設計書 7-1）
- ダイアログ・メッセージボックスはインターフェイス経由で呼び出し、単体テストでは偽の実装に差し替える

## 命名の方針

- メソッド・コマンドの名前は、何に対する操作かが分かる名前にする（例：`CreateNewDocument`、`OpenFileAsync`、`OpenFileCommand`、`UndoLastEdit`）
- `New`・`Open`のように対象が分からない名前や、C# の予約語・定型的な名前と同じ名前は使わない
- 非同期のメソッドは、名前の末尾を`Async`とし、`Task`を返す

## アクセス修飾子の方針

`FileParserLib`と同じ方針とする。

| 対象 | 修飾子 | 理由 |
|---|---|---|
| WPF のプロジェクト（`FileParseEditor`）から使う型（`MainViewModel`、一覧の行、サービスのインターフェイスと`NewKeyInput`、`ObservableObject`） | `public` | 画面から使うため |
| それ以外の`FileParseEditor.Core`の型（`Models`の型、コマンド、`EditDialogFlow`など） | `internal` | 画面から使えないようにし、内部を変更しても画面に影響しないようにするため |
| `internal`の型のメンバー | `internal` | 型が非公開であることを、メンバーを見ただけで分かるようにするため |
| 型の中だけで使うメソッド・フィールド | `private` | 変更の影響範囲を型の中に閉じるため |
| 継承を想定しないクラス | `sealed`（または`static`） | 意図しない継承を防ぐため |

- `ICommand`などインターフェイスの実装と、メソッドのオーバーライドは、C# の言語仕様上`public`とする
- 単体テストから`internal`の型をテストできるよう、`FileParseEditor.Core.csproj`で`InternalsVisibleTo`を指定している

## コレクションと一覧の更新

- 作る前に件数が決まる一覧の行は配列で作る。件数が増減するものだけ`List<T>`等を使い、理由をコメントに書く（`FileParserLib`と同じ方針）
- 画面の一覧（セクション・キー・診断情報）は`BulkObservableCollection<T>`で保持し、作り直すときは`ReplaceAll`で中身をまとめて入れ替える。`ObservableCollection<T>`は要素1つごとに画面へ通知するため、通知を1回にまとめて表示が遅くならないようにする

## 非同期処理

- ファイルの読み書きは、画面が止まらないよう別のスレッドで行い（`Task.Run`）、完了を`await`で待つ。画面に表示するものは、`await`の後（画面のスレッド）で変更する
- 読み書き中は`MainViewModel.IsBusy`を`true`にし、全ての操作を実行できなくする（マウスカーソルは待機中の表示になる）
- ウィンドウを閉じる前の確認・保存も非同期のため、`MainWindow`の`Closing`ではいったん閉じるのを取り消し、閉じてよいと分かってから改めて閉じる

## ビルドとテスト

```
dotnet build
dotnet test
```

- 対象フレームワーク：.NET 8（アプリは`net8.0-windows`、それ以外は`net8.0`）
- アプリの実行は Windows のみ。`FileParseEditor.Core`と単体テストは Windows 以外でも実行できる
- 文字コードは UTF-8（BOMなし）、改行コードは LF（`.editorconfig`と`.gitattributes`で指定）
