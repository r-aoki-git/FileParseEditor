using System.Text;

namespace FileParseEditor.Tests.Helpers;

/// <summary>
/// テスト用の一時フォルダ。Dispose で中のファイルごと削除する。
/// テストごとに別のフォルダを使うため、並列に実行しても互いに影響しない。
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    internal TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FileParseEditor.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    /// <summary>文字列を UTF-8（BOMなし）でファイルに書き込み、そのパスを返す。</summary>
    internal string WriteText(string fileName, string content)
    {
        var path = System.IO.Path.Combine(Path, fileName);
        File.WriteAllBytes(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content));
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
