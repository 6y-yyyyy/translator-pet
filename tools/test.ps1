param([string]$ReleaseDirectory = '', [switch]$WithModels)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $ReleaseDirectory) { $ReleaseDirectory = Join-Path $repoRoot 'dist\TranslatorPet' }
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
$exePath = Join-Path $ReleaseDirectory 'TranslatorPet.exe'
$testOutput = Join-Path $repoRoot 'artifacts\ui-test'
New-Item -ItemType Directory -Path $testOutput -Force | Out-Null
$previewArgs = '--preview "' + $testOutput + '"'
$previewProcess = Start-Process -FilePath $exePath -ArgumentList $previewArgs -WindowStyle Hidden -PassThru
if (-not $previewProcess.WaitForExit(30000)) { Stop-Process -Id $previewProcess.Id; throw 'UI test timed out.' }
if ($previewProcess.ExitCode -ne 0) { throw 'UI test failed. Check data/diagnostics.txt.' }
Get-Content -LiteralPath (Join-Path $testOutput 'ui-verification.txt')
if ($WithModels) {
    $pythonPath = Join-Path $ReleaseDirectory 'runtime\python.exe'
    $env:TRANSLATORPET_TEST_RELEASE = $ReleaseDirectory
    & $pythonPath -X utf8 (Join-Path $repoRoot 'tests\test_worker.py') -v
    if ($LASTEXITCODE -ne 0) { throw 'Offline engine tests failed.' }
    $frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
    $interopPath = Join-Path $ReleaseDirectory 'TranslatorPet.IntegrationTests.exe'
    & (Join-Path $frameworkPath 'csc.exe') /nologo /utf8output /target:exe /platform:x64 ('/reference:' + $exePath) ('/out:' + $interopPath) (Join-Path $repoRoot 'tests\InteropTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Interop test compilation failed.' }
    & $interopPath
    if ($LASTEXITCODE -ne 0) { throw 'Interop tests failed.' }
}
