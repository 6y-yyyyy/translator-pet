param([string]$OutputDirectory = '', [string]$CacheDirectory = '', [switch]$WithModel)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'dist\TranslatorPet' }
if (-not $CacheDirectory) { $CacheDirectory = Join-Path $repoRoot '.cache' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$CacheDirectory = [IO.Path]::GetFullPath($CacheDirectory)
New-Item -ItemType Directory -Path $CacheDirectory -Force | Out-Null
$lock = Get-Content -LiteralPath (Join-Path $repoRoot 'dependencies.lock.json') -Raw | ConvertFrom-Json
$pythonAsset = $lock.assets | Where-Object { $_.kind -eq 'python' }
$archivePath = Join-Path $CacheDirectory $pythonAsset.name
if (-not (Test-Path -LiteralPath $archivePath) -or (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pythonAsset.sha256) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -UseBasicParsing -Uri $pythonAsset.url -OutFile $archivePath
}
if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pythonAsset.sha256) { throw 'Python archive checksum mismatch.' }
# A separate bootstrap interpreter avoids overwriting a running runtime executable.
$bootstrapPath = Join-Path $CacheDirectory 'bootstrap-python'
Expand-Archive -LiteralPath $archivePath -DestinationPath $bootstrapPath -Force
$modelArgs = @()
if ($WithModel) { $modelArgs = @('--with-model') }
& (Join-Path $bootstrapPath 'python.exe') -I -X utf8 (Join-Path $PSScriptRoot 'prepare_runtime.py') --output $OutputDirectory --cache $CacheDirectory @modelArgs
if ($LASTEXITCODE -ne 0) { throw 'Runtime/model preparation failed.' }
