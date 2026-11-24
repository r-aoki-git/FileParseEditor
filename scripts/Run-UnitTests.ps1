<#
.SYNOPSIS
    単体テストをカバレッジ付きで実行し、検証ログの「5. 単体テストの結果」を書き換える。

.DESCRIPTION
    1. FileParseEditor.sln の全テストを、カバレッジ付きで実行する
    2. 件数・テストクラスごとの件数・カバレッジ・実行環境を集計する
    3. docs/FileParseEditor_検証ログ.md の、次の2行の間だけを書き換える（それ以外は変更しない）
         <!-- 単体テストの結果（自動）：ここから -->
         <!-- 単体テストの結果（自動）：ここまで -->

    失敗したテストがある場合は、検証ログに失敗したテストの一覧を書き込み、終了コード 1 で終わる。

.PARAMETER Executor
    検証ログに記入する実施者の名前。省略した場合は「（未記入）」とする。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Run-UnitTests.ps1 -Executor "青木蓮"
#>
param(
    [string]$Executor = ""
)

$ErrorActionPreference = 'Stop'
$invariant = [System.Globalization.CultureInfo]::InvariantCulture
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$root = Split-Path -Parent $PSScriptRoot
$solution = [System.IO.Path]::Combine($root, 'FileParseEditor.sln')
$logPath = [System.IO.Path]::Combine($root, 'docs', 'FileParseEditor_検証ログ.md')
$work = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), 'FileParseEditor.UnitTests.' + [Guid]::NewGuid().ToString('N'))

$startMarker = '<!-- 単体テストの結果（自動）：ここから -->'
$endMarker = '<!-- 単体テストの結果（自動）：ここまで -->'
$testNamespace = 'FileParseEditor.Tests.'

# ---- 補助関数 ----

function Get-TextOrEmpty($node) {
    if ($null -eq $node) { return '' }
    if ($node -is [string]) { return $node }
    return $node.InnerText
}

function Format-Percent([string]$rate) {
    return ([double]::Parse($rate, $invariant) * 100).ToString('0.0', $invariant) + '%'
}

function Get-OsDescription {
    if ($env:OS -eq 'Windows_NT') {
        $os = Get-CimInstance -ClassName Win32_OperatingSystem
        return "$($os.Caption)（バージョン $($os.Version)）"
    }
    return [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
}

function Get-Commit {
    try {
        $commit = (& git -C $root rev-parse --short HEAD).Trim()
        # 追跡していないファイル（エビデンスの画像など）はテストの対象に影響しないため、変更として数えない
        $changes = & git -C $root status --porcelain --untracked-files=no
        if ($changes) { $commit += '（未コミットの変更あり）' }
        return $commit
    }
    catch {
        return '（不明）'
    }
}

# 表のセルに書く文字列にする（| と改行は表を崩すため置き換える）
function ConvertTo-Cell([string]$text) {
    return $text.Replace("`r`n", ' ').Replace("`n", ' ').Replace('|', '\|')
}

# ---- 前提の確認 ----

if (-not [System.IO.File]::Exists($logPath)) { throw "検証ログが見つかりません: $logPath" }
$log = [System.IO.File]::ReadAllText($logPath, [System.Text.Encoding]::UTF8)
$start = $log.IndexOf($startMarker)
$end = $log.IndexOf($endMarker)
if ($start -lt 0 -or $end -lt $start) {
    throw "検証ログに、書き換える範囲の目印（$startMarker と $endMarker）が見つかりません"
}

New-Item -ItemType Directory -Path $work | Out-Null
try {
    # ---- 1. テストの実行 ----
    Write-Host '単体テストをカバレッジ付きで実行しています...' -ForegroundColor Cyan
    & dotnet test $solution '--collect:XPlat Code Coverage' '--logger' 'trx;LogFileName=unit.trx' '--results-directory' $work | Out-Host
    $testExit = $LASTEXITCODE

    $trxFile = Get-ChildItem -Path $work -Recurse -Filter '*.trx' | Select-Object -First 1
    if ($null -eq $trxFile) { throw "テスト結果（.trx）が見つかりません。ビルドに失敗した可能性があります（上の出力を確認すること）" }
    $trx = [xml][System.IO.File]::ReadAllText($trxFile.FullName, [System.Text.Encoding]::UTF8)
    $counters = $trx.TestRun.ResultSummary.Counters
    $total = [int]$counters.total
    $passed = [int]$counters.passed
    $failed = [int]$counters.failed

    # ---- 2. 集計 ----
    # テストクラスごとの件数（Theory は入力ごとに1件と数える）
    $classes = @{}
    $failures = New-Object System.Collections.Generic.List[string]
    foreach ($result in @($trx.TestRun.Results.UnitTestResult)) {
        $name = $result.testName
        $paren = $name.IndexOf('(')
        $method = if ($paren -ge 0) { $name.Substring(0, $paren) } else { $name }
        $class = $method.Substring(0, $method.LastIndexOf('.')).Replace($testNamespace, '')
        if (-not $classes.ContainsKey($class)) { $classes[$class] = @(0, 0) }
        $counts = $classes[$class]
        $counts[0]++
        if ($result.outcome -eq 'Passed') {
            $counts[1]++
        }
        else {
            $message = Get-TextOrEmpty $result.Output.ErrorInfo.Message
            $failures.Add('| ' + (ConvertTo-Cell $name.Replace($testNamespace, '')) + ' | ' + (ConvertTo-Cell $message) + ' |')
        }
        $classes[$class] = $counts
    }

    $coverageFile = Get-ChildItem -Path $work -Recurse -Filter 'coverage.cobertura.xml' | Select-Object -First 1
    $lineCoverage = '（取得できなかった）'
    $branchCoverage = '（取得できなかった）'
    if ($null -ne $coverageFile) {
        $coverage = ([xml][System.IO.File]::ReadAllText($coverageFile.FullName, [System.Text.Encoding]::UTF8)).coverage
        $lineCoverage = "$(Format-Percent $coverage.'line-rate')（$($coverage.'lines-valid')行中$($coverage.'lines-covered')行）"
        $branchCoverage = "$(Format-Percent $coverage.'branch-rate')（$($coverage.'branches-valid')分岐中$($coverage.'branches-covered')分岐）"
    }

    $runnerOutput = Get-TextOrEmpty $trx.TestRun.ResultSummary.Output.StdOut
    $runtime = [regex]::Match($runnerOutput, '\.NET (\d+\.\d+\.\d+)').Groups[1].Value
    $sdk = (& dotnet --version).Trim()
    $environment = "$(Get-OsDescription) ／ .NET SDK $sdk（ランタイム $runtime）"
    $date = (Get-Date).ToString("yyyy-MM-dd HH:mm 'UTC'zzz", $invariant)
    $executorText = if ($Executor.Length -gt 0) { $Executor } else { '（未記入）' }
    $resultText = if ($failed -eq 0 -and $testExit -eq 0) { "**$total 件中 $passed 件成功、$failed 件失敗**" } else { "**$total 件中 $passed 件成功、$failed 件失敗（要確認）**" }

    # ---- 3. 検証ログの書き換え ----
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add($startMarker)
    $lines.Add('')
    $lines.Add('| 項目 | 内容 |')
    $lines.Add('|---|---|')
    $lines.Add("| 実行日時 | $date |")
    $lines.Add("| 実施者 | $executorText |")
    $lines.Add("| 対象コミット | $(Get-Commit) |")
    $lines.Add("| 実行環境 | $environment |")
    $lines.Add("| 結果 | $resultText |")
    $lines.Add("| 行カバレッジ（FileParseEditor.Core） | $lineCoverage |")
    $lines.Add("| 分岐カバレッジ（FileParseEditor.Core） | $branchCoverage |")
    $lines.Add('')
    $lines.Add('テストクラスごとの件数（テスト仕様書の「単体テスト」シートと対応する）')
    $lines.Add('')
    $lines.Add('| テストクラス | 件数 | 成功 |')
    $lines.Add('|---|---|---|')
    foreach ($class in ($classes.Keys | Sort-Object)) {
        $counts = $classes[$class]
        $lines.Add("| $class | $($counts[0]) | $($counts[1]) |")
    }
    if ($failures.Count -gt 0) {
        $lines.Add('')
        $lines.Add('失敗したテスト')
        $lines.Add('')
        $lines.Add('| テスト | エラーメッセージ |')
        $lines.Add('|---|---|')
        foreach ($line in $failures) { $lines.Add($line) }
    }
    $lines.Add('')
    $lines.Add('カバレッジは coverlet で計測した（`dotnet test --collect:"XPlat Code Coverage"`）。WPF のプロジェクト（画面）は単体テストの対象外のため、画面の操作は機能テスト・エラーケーステストで確認する。')
    $lines.Add('')
    $lines.Add($endMarker)

    $newline = if ($log.Contains("`r`n")) { "`r`n" } else { "`n" }
    $block = [string]::Join($newline, $lines)
    $updated = $log.Substring(0, $start) + $block + $log.Substring($end + $endMarker.Length)
    [System.IO.File]::WriteAllText($logPath, $updated, $utf8NoBom)

    Write-Host ''
    Write-Host "結果: $total 件中 $passed 件成功、$failed 件失敗" -ForegroundColor Cyan
    Write-Host "検証ログの「5. 単体テストの結果」を書き換えました: $logPath" -ForegroundColor Cyan
    if ($failed -gt 0 -or $testExit -ne 0) {
        Write-Warning '失敗したテストがあります。検証ログの「失敗したテスト」を確認し、6章に記入してください。'
        exit 1
    }
    Write-Host 'テスト仕様書の「単体テスト」シートに、確認日付と判定（済）を記入してください。' -ForegroundColor Green
}
finally {
    if ([System.IO.Directory]::Exists($work)) {
        Remove-Item -Path $work -Recurse -Force
    }
}
