param([string]$UnityPath = 'D:\Design_game\6000.3.24f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$stage = Join-Path $repo 'Temp/CombatVerification'
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity executable not found: $UnityPath. Supply -UnityPath." }
New-Item -ItemType Directory -Force "$stage/Assets/Editor", "$stage/Assets/Player", "$stage/Assets/Enemy", "$stage/Packages", "$stage/ProjectSettings" | Out-Null
Copy-Item -LiteralPath "$repo/Assets/Combat" -Destination "$stage/Assets" -Recurse -Force
Copy-Item -LiteralPath "$repo/Assets/Player/Scripts" -Destination "$stage/Assets/Player" -Recurse -Force
Copy-Item -LiteralPath "$repo/Assets/Player/Input" -Destination "$stage/Assets/Player" -Recurse -Force
Copy-Item -LiteralPath "$repo/Assets/Enemy/Scripts" -Destination "$stage/Assets/Enemy" -Recurse -Force
Copy-Item -LiteralPath "$PSScriptRoot/CombatIntegrationVerification.cs" -Destination "$stage/Assets/Editor/CombatIntegrationVerification.cs" -Force
Copy-Item -LiteralPath "$PSScriptRoot/CombatVerificationDriver.cs" -Destination "$stage/Assets/CombatVerificationDriver.cs" -Force
Copy-Item -LiteralPath "$repo/ProjectSettings/ProjectVersion.txt" -Destination "$stage/ProjectSettings" -Force
$ugui = (Get-ChildItem -LiteralPath "$repo/Library/PackageCache" -Directory | Where-Object Name -Like 'com.unity.ugui@*' | Select-Object -First 1).FullName.Replace('\', '/')
$input = (Join-Path $repo 'Packages/com.unity.inputsystem').Replace('\', '/')
$dependencies = @{
    'com.unity.inputsystem'="file:$input"; 'com.unity.ugui'="file:$ugui"
    'com.unity.modules.physics2d'='1.0.0'; 'com.unity.modules.animation'='1.0.0'
    'com.unity.modules.imageconversion'='1.0.0'; 'com.unity.modules.ui'='1.0.0'
    'com.unity.modules.uielements'='1.0.0'; 'com.unity.modules.imgui'='1.0.0'
}
@{ dependencies=$dependencies } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$stage/Packages/manifest.json"
$log = Join-Path $repo 'Temp/combat-unity-verification.log'
$result = Join-Path $stage 'combat-verification-results.txt'
Set-Content -LiteralPath $result -Value ''
$args = @('-batchmode', '-nographics', '-projectPath', ('"' + $stage + '"'), '-executeMethod', 'CombatIntegrationVerification.Run', '-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $UnityPath -ArgumentList $args -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(120000)) {
    $process.Kill()
    throw "Unity verification timed out. See $log"
}
if (Test-Path -LiteralPath $result) { Get-Content -LiteralPath $result }
if ($process.ExitCode -ne 0) { throw "Unity verification failed ($($process.ExitCode)). See $log" }
if (!(Test-Path -LiteralPath $result)) { throw "Unity produced no verification result. See $log" }
if ((Get-Content -LiteralPath $result | Select-Object -Last 1) -ne 'PASS: all Unity combat integration checks.') { throw "Unity produced an incomplete verification result. See $log" }
