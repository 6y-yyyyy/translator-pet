param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'dist\TranslatorPet' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkPath 'csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'Windows x64 .NET Framework 4.8 is required.' }
$referenceArgs = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Windows.Forms.dll','System.Drawing.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $frameworkPath $_) }
$sourceRoot = Join-Path $repoRoot 'src\TranslatorPet'
$sourceFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' | ForEach-Object FullName)
$resourceArg = '/resource:' + (Join-Path $sourceRoot 'TranslationWindow.xaml') + ',TranslatorPet.TranslationWindow.xaml'
$manifestArg = '/win32manifest:' + (Join-Path $sourceRoot 'app.manifest')
& $compilerPath /nologo /target:winexe /platform:x64 /optimize+ /utf8output ('/out:' + (Join-Path $OutputDirectory 'TranslatorPet.exe')) $manifestArg $resourceArg $referenceArgs $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item -LiteralPath (Join-Path $sourceRoot 'App.config') -Destination (Join-Path $OutputDirectory 'TranslatorPet.exe.config')
Set-Content -LiteralPath (Join-Path $OutputDirectory 'portable.marker') -Value 'Portable profile lives in ./data' -Encoding ascii
Write-Output ('Built: ' + (Join-Path $OutputDirectory 'TranslatorPet.exe'))
