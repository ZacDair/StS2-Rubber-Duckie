param(
    [string] $GodotPath,
    [string] $Sts2Path,
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $projectRoot 'RubberDuckie.csproj'
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source

function Assert-ExitCode([string] $Step) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

$buildProperties = @()
if ($Sts2Path) { $buildProperties += "-p:Sts2Path=$Sts2Path" }
if ($GodotPath) { $buildProperties += "-p:GodotPath=$GodotPath" }
$propertyJson = & $dotnet msbuild $project -nologo '-getProperty:GodotPath,Sts2Path,Sts2DataDir' @buildProperties
Assert-ExitCode 'Resolving build paths'
$properties = ($propertyJson -join "`n" | ConvertFrom-Json).Properties
$GodotPath = $properties.GodotPath
$Sts2Path = $properties.Sts2Path
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) {
    throw "Megadot executable not found: $GodotPath"
}
$consoleGodot = Join-Path (Split-Path -Parent $GodotPath) `
    "$([IO.Path]::GetFileNameWithoutExtension($GodotPath))_console.exe"
if (Test-Path -LiteralPath $consoleGodot) { $GodotPath = $consoleGodot }

$manifest = Get-Content (Join-Path $projectRoot 'RubberDuckie.json') -Raw | ConvertFrom-Json
if ($manifest.version -notmatch '^v\d+\.\d+\.\d+$') { throw 'Invalid release version.' }
$version = $manifest.version
$projectXml = [xml](Get-Content $project -Raw)
$assemblyVersion = $projectXml.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
if ($version -ne "v$assemblyVersion") { throw 'DLL and manifest versions disagree.' }

if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'artifacts' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$null = New-Item -ItemType Directory -Force -Path $OutputDirectory
Set-Content -LiteralPath (Join-Path $OutputDirectory '.gdignore') -Value ''
$stageRoot = Join-Path $OutputDirectory "stage-$version"
if (Test-Path -LiteralPath $stageRoot) { Remove-Item -LiteralPath $stageRoot -Recurse -Force }
$modDirectory = Join-Path $stageRoot 'RubberDuckie'
$null = New-Item -ItemType Directory -Force -Path $modDirectory
$buildProperties += "-p:ModsPath=$stageRoot/"
$testLog = Join-Path $OutputDirectory "regression-$version.log"
$previousDotnetRoot = [Environment]::GetEnvironmentVariable('DOTNET_ROOT')
$previousDotnetRootX64 = [Environment]::GetEnvironmentVariable('DOTNET_ROOT_X64')

Push-Location $projectRoot
try {
    & $dotnet build (Join-Path $projectRoot 'tests/RubberDuckie.RegressionTests.csproj') `
        -c Debug @buildProperties
    Assert-ExitCode 'Building combat regression tests'

    # Megadot otherwise selects the newest installed runtime. The game's Harmony
    # version supports .NET 9, so expose only that runtime to this process.
    $dotnetDirectory = Split-Path -Parent $dotnet
    $runtime9 = Get-ChildItem (Join-Path $dotnetDirectory 'shared/Microsoft.NETCore.App') -Directory `
        | Where-Object Name -Like '9.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    $host9 = Get-ChildItem (Join-Path $dotnetDirectory 'host/fxr') -Directory `
        | Where-Object Name -Like '9.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    if (-not $runtime9 -or -not $host9) { throw 'Install the .NET 9 SDK before running these tests.' }
    $runtimeRoot = Join-Path ([IO.Path]::GetTempPath()) 'RubberDuckie-dotnet9'
    $runtimeParent = Join-Path $runtimeRoot 'shared/Microsoft.NETCore.App'
    $hostParent = Join-Path $runtimeRoot 'host/fxr'
    $null = New-Item -ItemType Directory -Force -Path $runtimeParent, $hostParent
    foreach ($link in @(
        @((Join-Path $runtimeParent $runtime9.Name), $runtime9.FullName),
        @((Join-Path $hostParent $host9.Name), $host9.FullName)
    )) {
        if (-not (Test-Path -LiteralPath $link[0])) {
            $null = New-Item -ItemType Junction -Path $link[0] -Target $link[1]
        }
    }
    $env:DOTNET_ROOT = $runtimeRoot
    $env:DOTNET_ROOT_X64 = $runtimeRoot
    & $GodotPath --headless --audio-driver Dummy --path (Join-Path $projectRoot 'tests') --quit-after 3600 `
        | Tee-Object -FilePath $testLog
    Assert-ExitCode 'Combat regression tests'
    $testReport = [regex]::Match((Get-Content $testLog -Raw), '(\d+)/(\d+) regression tests passed\.')
    if (-not $testReport.Success -or $testReport.Groups[1].Value -ne $testReport.Groups[2].Value) {
        throw 'The combat tests did not report a complete successful run.'
    }

    $previousInnerExport = [Environment]::GetEnvironmentVariable('IsInnerGodotExport')
    try {
        $env:IsInnerGodotExport = 'true'
        & $GodotPath --headless --path $projectRoot --editor --import
        Assert-ExitCode 'Importing release assets'
        & $GodotPath --headless --path $projectRoot --export-pack BasicExport `
            (Join-Path $modDirectory 'RubberDuckie.pck')
        Assert-ExitCode 'Exporting release PCK'
    }
    finally {
        [Environment]::SetEnvironmentVariable('IsInnerGodotExport', $previousInnerExport)
    }

    $smokeDirectory = Join-Path $OutputDirectory 'pack-smoke'
    $null = New-Item -ItemType Directory -Force -Path $smokeDirectory
    Set-Content -LiteralPath (Join-Path $smokeDirectory 'project.godot') -Encoding Ascii `
        -Value "config_version=5`n[application]`nconfig/name=`"RubberDuckie Pack Test`""
    Copy-Item -LiteralPath (Join-Path $projectRoot 'tests/VerifyPack.gd') -Destination $smokeDirectory -Force
    & $GodotPath --headless --audio-driver Dummy --path $smokeDirectory --script res://VerifyPack.gd `
        -- (Join-Path $modDirectory 'RubberDuckie.pck')
    Assert-ExitCode 'Release PCK playback test'

    $files = @(Get-ChildItem -LiteralPath $modDirectory -File | Sort-Object Name)
    $expected = @('RubberDuckie.dll', 'RubberDuckie.json', 'RubberDuckie.pck')
    if (($files.Name -join ',') -ne ($expected -join ',')) { throw 'Unexpected release files.' }
    if (@($files | Where-Object Length -eq 0).Count -ne 0) { throw 'An empty file was produced.' }
    $dllVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $modDirectory 'RubberDuckie.dll')).Version
    if ($dllVersion.ToString(3) -ne $assemblyVersion) { throw 'Built DLL version is incorrect.' }
    $testedDll = Join-Path $projectRoot 'tests/.godot/mono/temp/bin/Debug/RubberDuckie.dll'
    if ((Get-FileHash $testedDll).Hash -ne (Get-FileHash (Join-Path $modDirectory 'RubberDuckie.dll')).Hash) {
        throw 'The packaged DLL differs from the DLL used by the combat tests.'
    }

    $archive = Join-Path $OutputDirectory "RubberDuckie-$version.zip"
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
    $zipWriter = [IO.Compression.ZipFile]::Open($archive, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zipWriter, $file.FullName, "RubberDuckie/$($file.Name)", [IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally { $zipWriter.Dispose() }
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entries = @($zip.Entries | Where-Object { -not $_.FullName.EndsWith('/') } | Sort-Object FullName)
        $expectedEntries = @($expected | ForEach-Object { "RubberDuckie/$_" })
        if (($entries.FullName -join ',') -ne ($expectedEntries -join ',')) {
            throw 'The archive has an incorrect installation layout.'
        }
    }
    finally { $zip.Dispose() }

    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath "$archive.sha256" -Encoding Ascii -Value "$hash  $([IO.Path]::GetFileName($archive))"
    $gameVersion = (Get-Content (Join-Path $Sts2Path 'release_info.json') -Raw | ConvertFrom-Json).version
    Write-Output "Release ready: $archive"
    Write-Output "Game tested: $gameVersion"
    Write-Output "SHA256: $hash"
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ROOT', $previousDotnetRoot)
    [Environment]::SetEnvironmentVariable('DOTNET_ROOT_X64', $previousDotnetRootX64)
    Pop-Location
}
