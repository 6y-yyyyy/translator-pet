param([string]$ReleaseDirectory = '', [string]$Destination = '', [switch]$IncludeModel)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $ReleaseDirectory) { $ReleaseDirectory = Join-Path $repoRoot 'dist\TranslatorPet' }
if (-not $Destination) { $Destination = Join-Path $repoRoot 'artifacts\TranslatorPet-v0.3.1-windows-x64.zip' }
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
$Destination = [IO.Path]::GetFullPath($Destination)
foreach ($required in @('TranslatorPet.exe','TranslatorPet.exe.config','runtime\python.exe','engine\worker.py','engine\model_store.py','model.json','LICENSE','THIRD_PARTY_NOTICES.md','licenses\Qwen3-Apache-2.0.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $ReleaseDirectory $required))) { throw ('Missing release asset: ' + $required) }
}
if ($IncludeModel) {
    $model = Get-Content -LiteralPath (Join-Path $ReleaseDirectory 'model.json') -Raw -Encoding utf8 | ConvertFrom-Json
    if ([IO.Path]::GetFileName($model.name) -ne $model.name) { throw 'Invalid model filename.' }
    $modelPath = Join-Path $ReleaseDirectory ('models\' + $model.name)
    if (-not (Test-Path -LiteralPath $modelPath) -or (Get-FileHash -LiteralPath $modelPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $model.sha256) { throw 'The complete verified model is required for -IncludeModel.' }
}
New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
if (Test-Path -LiteralPath $Destination) { throw 'Destination already exists; use a new archive filename.' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($Destination, [IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -LiteralPath $ReleaseDirectory -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($ReleaseDirectory.Length + 1)
        if ($relative -notmatch '(^|\\)(data|__pycache__|preview)(\\|$)' -and $relative -notmatch 'IntegrationTests|\.pyc$|diagnostics\.txt$|\.part$' -and ($IncludeModel -or $relative -notmatch '^models\\')) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, 'TranslatorPet/' + $relative.Replace('\','/'), [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
} finally { $zip.Dispose() }
Get-FileHash -LiteralPath $Destination -Algorithm SHA256 | Format-List
