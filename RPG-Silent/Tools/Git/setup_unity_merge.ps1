param(
    [Parameter(Mandatory = $true)]
    [string]$UnityEditorPath
)

$ErrorActionPreference = 'Stop'
$editor = Get-Item -LiteralPath $UnityEditorPath
$mergeTool = Join-Path $editor.Directory.FullName 'Data\Tools\UnityYAMLMerge.exe'
if (-not (Test-Path -LiteralPath $mergeTool -PathType Leaf)) {
    throw "找不到 UnityYAMLMerge：$mergeTool"
}
$repository = (& git -C $PSScriptRoot rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0) { throw '找不到工具所在的 Git 仓库。' }

# The dollar variables below are literal placeholders interpreted later by git mergetool.
$command = '"' + $mergeTool.Replace('\', '/') + '" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
$settings = [ordered]@{
    'merge.tool' = 'unityyamlmerge'
    'mergetool.unityyamlmerge.trustExitCode' = 'false'
    'mergetool.unityyamlmerge.cmd' = $command
}
foreach ($key in $settings.Keys) {
    & git -C $repository config --local $key $settings[$key]
    if ($LASTEXITCODE -ne 0) { throw "写入仓库 Git 配置失败：$key" }
}
Write-Output "已为此仓库配置 UnityYAMLMerge：$mergeTool"
Write-Output '发生场景或预制体冲突时使用 git mergetool，并在 Unity 中复核结果。'
