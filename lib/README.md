# lib

アプリが参照する外部のアセンブリを置くフォルダ（基本設計書 7-2）。

| ファイル | 内容 |
|---|---|
| `FileParserLib.dll` | LFCファイル読み書きライブラリ。`FileParserLib`リポジトリの`main`（コミット `7f0df8f`）を`dotnet build -c Release`でビルドしたもの（対象フレームワーク：`net8.0`） |

## 差し替える場合

1. `FileParserLib`リポジトリで、差し替えるコミットを取得する
2. `dotnet build FileParserLib/FileParserLib.csproj -c Release` でビルドする
3. `FileParserLib/bin/Release/net8.0/FileParserLib.dll` をこのフォルダに上書きでコピーする
4. 上の表のコミットを書き換え、アプリをビルドして全てのテストが成功することを確認する
