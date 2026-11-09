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
├── Mvvm/                     MVVM の基盤（ObservableObject、RelayCommand）
├── Models/                   文書の管理（DocumentSession）、値の表記の変換（LfcValueNotation）
├── ViewModels/               画面の表示内容とコマンド（MainViewModel）、一覧の1行、メッセージの文言
└── Services/                 ダイアログ・メッセージのインターフェイス

FileParseEditor.Tests/      単体テスト（xUnit、net8.0）。フォルダ構成は FileParseEditor.Core と同じにする
└── Helpers/                  テスト用の偽のサービス、一時フォルダ

lib/                        参照する FileParserLib.dll（どのコミットからビルドしたかは lib/README.md）
docs/                       要件定義書・基本設計書・スケジュール・作図用 PlantUML
diagrams/                   作図用 PlantUML を図ごとに分けたファイル
```

- ViewModel を WPF に依存しない`FileParseEditor.Core`に置き、ViewModel から画面の部品を使えないようにしている（基本設計書 7-1）
- ダイアログ・メッセージボックスはインターフェイス経由で呼び出し、単体テストでは偽の実装に差し替える

## ビルドとテスト

```
dotnet build
dotnet test
```

- 対象フレームワーク：.NET 8（アプリは`net8.0-windows`、それ以外は`net8.0`）
- アプリの実行は Windows のみ。`FileParseEditor.Core`と単体テストは Windows 以外でも実行できる
- 文字コードは UTF-8（BOMなし）、改行コードは LF（`.editorconfig`と`.gitattributes`で指定）
