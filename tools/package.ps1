param([string]$ReleaseDirectory = '', [string]$Destination = '')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $ReleaseDirectory) { $ReleaseDirectory = Join-Path $repoRoot 'dist\TranslatorPet' }
if (-not $Destination) { $Destination = Join-Path $repoRoot 'artifacts\TranslatorPet-v0.2.0-windows-x64.zip' }
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
$Destination = [IO.Path]::GetFullPath($Destination)
foreach ($required in @('TranslatorPet.exe','TranslatorPet.exe.config','runtime\python.exe','engine\worker.py','models\en_zh\model\model.bin','models\zh_en\model\model.bin','LICENSE','THIRD_PARTY_NOTICES.md','licenses\CC-BY-4.0.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $ReleaseDirectory $required))) { throw ('Missing release asset: ' + $required) }
}
New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
if (Test-Path -LiteralPath $Destination) { throw 'Destination already exists; use a new archive filename.' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($Destination, [IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -LiteralPath $ReleaseDirectory -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($ReleaseDirectory.Length + 1)
        if ($relative -notmatch '(^|\\)(data|__pycache__|preview)(\\|$)' -and $relative -notmatch 'IntegrationTests|\.pyc$|diagnostics\.txt$') {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, 'TranslatorPet/' + $relative.Replace('\','/'), [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
} finally { $zip.Dispose() }
Get-FileHash -LiteralPath $Destination -Algorithm SHA256 | Format-List
