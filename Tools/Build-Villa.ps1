[CmdletBinding()]
param(
    [switch]$Prepare,
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$villaProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$villaSourceScene = Join-Path $villaProjectRoot 'Assets\_LevelProject\Scenes 1\Industrial_Level_01.unity'
$villaSourceHash = (Get-FileHash -LiteralPath $villaSourceScene -Algorithm SHA256).Hash
if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw "Unity editor not found: $UnityEditorPath" }
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'Close the Unity Editor before running the batch build. No existing editor will be stopped.' }

# Preserve project-wide files that Unity can migrate or rewrite while preparing a player.
# New FPS assets, the generated scene, build settings, and the four FPS layers remain saved.
$villaPreservedFiles = @(
    'Assets\Settings\DefaultVolumeProfile.asset',
    'Assets\Settings\UniversalRenderPipelineGlobalSettings.asset',
    'ProjectSettings\GraphicsSettings.asset',
    'ProjectSettings\QualitySettings.asset',
    'ProjectSettings\ProjectSettings.asset',
    'ProjectSettings\UnityConnectSettings.asset',
    'ProjectSettings\Packages\com.unity.probuilder\Settings.json'
)
$villaSnapshots = @{}
foreach ($villaRelativePath in $villaPreservedFiles) {
    $villaAbsolutePath = Join-Path $villaProjectRoot $villaRelativePath
    if (Test-Path -LiteralPath $villaAbsolutePath -PathType Leaf) {
        $villaSnapshots[$villaAbsolutePath] = [IO.File]::ReadAllBytes($villaAbsolutePath)
    }
}

$villaLogFolder = Join-Path $villaProjectRoot 'Logs\VillaFPS'
[IO.Directory]::CreateDirectory($villaLogFolder) | Out-Null
$villaLogPath = Join-Path $villaLogFolder ('build-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '.log')
$villaMethod = if ($Prepare) { 'VillaBuildTools.PrepareAndBuild' } else { 'VillaBuildTools.BuildWindows' }
$villaArguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + $villaProjectRoot + '"'),'-executeMethod',$villaMethod,'-logFile',('"' + $villaLogPath + '"'))
$villaBuildProcess = $null
try {
    Write-Output "Building with $villaMethod. Play mode and the player will not be started."
    $villaBuildProcess = Start-Process -FilePath $UnityEditorPath -ArgumentList $villaArguments -WorkingDirectory $villaProjectRoot -WindowStyle Hidden -PassThru
    $villaBuildProcess.WaitForExit()
    if ($villaBuildProcess.ExitCode -ne 0) { throw "Unity build exited with code $($villaBuildProcess.ExitCode). Log: $villaLogPath" }
}
finally {
    # Stop only this script's owned batch process if the script was interrupted while it ran.
    if ($null -ne $villaBuildProcess -and -not $villaBuildProcess.HasExited) {
        Stop-Process -Id $villaBuildProcess.Id -Force
        $villaBuildProcess.WaitForExit()
    }
    foreach ($villaSnapshot in $villaSnapshots.GetEnumerator()) {
        [IO.File]::WriteAllBytes($villaSnapshot.Key,$villaSnapshot.Value)
    }
}

$villaFinalHash = (Get-FileHash -LiteralPath $villaSourceScene -Algorithm SHA256).Hash
if ($villaFinalHash -ne $villaSourceHash) { throw 'The source villa scene changed during the build. Inspect the preparation report before continuing.' }
$villaSummary = Join-Path $villaLogFolder 'windows-build.txt'
if (Test-Path -LiteralPath $villaSummary) { Get-Content -LiteralPath $villaSummary }
Write-Output "Log: $villaLogPath"
Write-Output "Windows package: $(Join-Path $villaProjectRoot 'Builds\Villa_Operation')"
Write-Output 'Source scene and project-wide settings preserved. Manual gameplay and visual verification are pending.'
