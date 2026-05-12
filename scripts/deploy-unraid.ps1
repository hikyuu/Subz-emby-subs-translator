param(
    [string]$RemoteHost = "",
    [int]$RemotePort = 0,
    [string]$RemoteUser = "",
    [string]$RemotePassword = "",
    [string]$RemoteHostKey = "",
    [string]$ConfigPath = "",
    [int]$ConnectTimeoutSec = 15,
    [int]$CopyTimeoutSec = 120,
    [int]$RemoteCommandTimeoutSec = 60,
    [int]$RetryCount = 3,
    [int]$RetryDelaySec = 5
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $root "tmp\remote.unraid.json"
}

if (Test-Path $ConfigPath) {
    $remoteConfig = Get-Content -Raw $ConfigPath | ConvertFrom-Json
    if ($remoteConfig.unraid) {
        if ([string]::IsNullOrWhiteSpace($RemoteHost)) { $RemoteHost = [string]$remoteConfig.unraid.host }
        if ($RemotePort -le 0) { $RemotePort = [int]$remoteConfig.unraid.port }
        if ([string]::IsNullOrWhiteSpace($RemoteUser)) { $RemoteUser = [string]$remoteConfig.unraid.username }
        if ([string]::IsNullOrWhiteSpace($RemotePassword)) { $RemotePassword = [string]$remoteConfig.unraid.password }
        if ([string]::IsNullOrWhiteSpace($RemoteHostKey)) { $RemoteHostKey = [string]$remoteConfig.unraid.hostKey }
    }
}

if ([string]::IsNullOrWhiteSpace($RemoteHost)) { $RemoteHost = "sanding.life" }
if ($RemotePort -le 0) { $RemotePort = 55522 }
if ([string]::IsNullOrWhiteSpace($RemoteUser)) { $RemoteUser = "root" }

$sshTarget = "$RemoteUser@$RemoteHost"
$sshpass = Get-Command sshpass -ErrorAction SilentlyContinue
$useSshpass = -not [string]::IsNullOrWhiteSpace($RemotePassword) -and $null -ne $sshpass
$plinkCmd = Get-Command plink -ErrorAction SilentlyContinue
$pscpCmd = Get-Command pscp -ErrorAction SilentlyContinue
$usePuttyPassword = -not $useSshpass -and -not [string]::IsNullOrWhiteSpace($RemotePassword) -and $null -ne $plinkCmd -and $null -ne $pscpCmd
$hasPuttyHostKey = -not [string]::IsNullOrWhiteSpace($RemoteHostKey)

function Invoke-ToolWithTimeout {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$Arguments = @(),
        [int]$TimeoutSec = 30,
        [string]$StepName = "command"
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = ($Arguments -join " ")
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    if (-not $proc.Start()) {
        throw "Failed to start ${StepName}: $FilePath"
    }

    if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
        try { $proc.Kill() } catch {}
        throw "$StepName timed out after ${TimeoutSec}s"
    }

    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    if ($stdout) { Write-Host $stdout.TrimEnd() }
    if ($stderr) { Write-Host $stderr.TrimEnd() }

    if ($proc.ExitCode -ne 0) {
        throw "$StepName failed with exit code $($proc.ExitCode)"
    }
}

function Invoke-WithRetry {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$StepName,
        [int]$Attempts = 3,
        [int]$DelaySec = 5
    )

    if ($Attempts -lt 1) { $Attempts = 1 }
    if ($DelaySec -lt 0) { $DelaySec = 0 }

    $lastError = $null
    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            & $Action
            return
        } catch {
            $lastError = $_
            if ($i -lt $Attempts) {
                Write-Warning "$StepName failed (attempt $i/$Attempts): $($_.Exception.Message). Retrying in ${DelaySec}s..."
                Start-Sleep -Seconds $DelaySec
            }
        }
    }

    if ($lastError) { throw $lastError }
    throw "$StepName failed after $Attempts attempt(s)."
}

function Test-RemoteConnectivity {
    Write-Host "Checking remote connectivity..." -ForegroundColor DarkCyan
    $connectCmd = "echo SUBZ_CONNECT_OK"

    if ($usePuttyPassword) {
        $args = @("-batch", "-P", "$RemotePort", "-pw", "$RemotePassword")
        if ($hasPuttyHostKey) { $args += @("-hostkey", "$RemoteHostKey") }
        $args += @("$sshTarget", "$connectCmd")
        Invoke-ToolWithTimeout -FilePath "plink" -Arguments $args -TimeoutSec $ConnectTimeoutSec -StepName "remote connectivity check"
        return
    }

    if ($useSshpass) {
        $args = @("-p", "$RemotePassword", "ssh", "-p", "$RemotePort", "-o", "StrictHostKeyChecking=no", "$sshTarget", "$connectCmd")
        Invoke-ToolWithTimeout -FilePath "sshpass" -Arguments $args -TimeoutSec $ConnectTimeoutSec -StepName "remote connectivity check"
        return
    }

    $args = @("-p", "$RemotePort", "$sshTarget", "$connectCmd")
    Invoke-ToolWithTimeout -FilePath "ssh" -Arguments $args -TimeoutSec $ConnectTimeoutSec -StepName "remote connectivity check"
}

function Invoke-RemoteCommand {
    param([Parameter(Mandatory = $true)][string]$Command)

    if ($useSshpass) {
        & sshpass -p $RemotePassword ssh -p $RemotePort -o StrictHostKeyChecking=no $sshTarget $Command
        if ($LASTEXITCODE -ne 0) { throw "Remote command failed (sshpass/ssh): $Command" }
    } elseif ($usePuttyPassword) {
        if ($hasPuttyHostKey) {
            & plink -batch -P $RemotePort -pw $RemotePassword -hostkey "$RemoteHostKey" $sshTarget $Command
        } else {
            & plink -batch -P $RemotePort -pw $RemotePassword $sshTarget $Command
        }
        if ($LASTEXITCODE -ne 0) { throw "Remote command failed (plink): $Command" }
    } else {
        & ssh -p $RemotePort $sshTarget $Command
        if ($LASTEXITCODE -ne 0) { throw "Remote command failed (ssh): $Command" }
    }
}

function Copy-ToRemote {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if ($useSshpass) {
        $args = @("-p", "$RemotePassword", "scp", "-P", "$RemotePort", "-o", "StrictHostKeyChecking=no", "$Source", "${sshTarget}:$Destination")
        Invoke-ToolWithTimeout -FilePath "sshpass" -Arguments $args -TimeoutSec $CopyTimeoutSec -StepName "copy DLL to remote"
        return
    }

    if ($usePuttyPassword) {
        $args = @("-batch", "-P", "$RemotePort", "-pw", "$RemotePassword")
        if ($hasPuttyHostKey) { $args += @("-hostkey", "$RemoteHostKey") }
        $args += @("$Source", "${sshTarget}:$Destination")
        Invoke-ToolWithTimeout -FilePath "pscp" -Arguments $args -TimeoutSec $CopyTimeoutSec -StepName "copy DLL to remote"
        return
    }

    $scpArgs = @("-P", "$RemotePort", "$Source", "${sshTarget}:$Destination")
    Invoke-ToolWithTimeout -FilePath "scp" -Arguments $scpArgs -TimeoutSec $CopyTimeoutSec -StepName "copy DLL to remote"
}

Write-Host "=== Step 1: Build Plugin ===" -ForegroundColor Cyan
$buildScript = Join-Path $PSScriptRoot "build-plugin.ps1"
& $buildScript
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$dllPath = Join-Path $root "SubZ.Plugin.dll"
if (-not (Test-Path $dllPath)) { throw "DLL not found at $dllPath" }

Write-Host "=== Step 2: Copy DLL to Unraid ===" -ForegroundColor Cyan
Invoke-WithRetry -StepName "remote connectivity check" -Attempts $RetryCount -DelaySec $RetryDelaySec -Action { Test-RemoteConnectivity }
$remoteDir = "/tmp/subz_sync/"
Invoke-RemoteCommand "mkdir -p $remoteDir"
Invoke-WithRetry -StepName "copy DLL to remote" -Attempts $RetryCount -DelaySec $RetryDelaySec -Action { Copy-ToRemote $dllPath $remoteDir }

Write-Host "=== Step 3: Deploy on Unraid ===" -ForegroundColor Cyan
$deployScriptLocal = Join-Path $env:TEMP "subz-deploy.sh"
 $deployScriptContent = @'
#!/bin/sh
set -e
pluginDir=""
for d in /mnt/user/DockerFile/emby/plugins /mnt/user/appdata/binhex-emby/plugins /mnt/user/appdata/emby/plugins /config/plugins /mnt/cache/appdata/binhex-emby/plugins /mnt/cache/appdata/emby/plugins; do
  if [ -d "$d" ]; then pluginDir="$d"; break; fi
done
if [ -z "$pluginDir" ]; then
  echo "PLUGIN_DIR_NOT_FOUND"
  exit 2
fi
mkdir -p "$pluginDir"
cp -f /tmp/subz_sync/SubZ.Plugin.dll "$pluginDir/SubZ.Plugin.dll"
echo "Deployed to: $pluginDir/SubZ.Plugin.dll"
ls -l "$pluginDir/SubZ.Plugin.dll"
'@
$deployScriptContent = $deployScriptContent -replace "`r`n", "`n"
[System.IO.File]::WriteAllText($deployScriptLocal, $deployScriptContent, (New-Object System.Text.ASCIIEncoding))

$deployScriptRemote = "/tmp/subz_sync/subz-deploy.sh"
Copy-ToRemote $deployScriptLocal $deployScriptRemote
Invoke-RemoteCommand "chmod +x $deployScriptRemote && sh $deployScriptRemote"

Write-Host "=== Step 4: Restart Emby ===" -ForegroundColor Cyan
Invoke-RemoteCommand "docker restart embyserver 2>/dev/null || docker restart emby 2>/dev/null || docker restart binhex-emby 2>/dev/null || /etc/rc.d/rc.emby restart 2>/dev/null || echo 'Please restart Emby manually'"

Write-Host "=== Done! ===" -ForegroundColor Green
