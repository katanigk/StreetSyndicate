$ErrorActionPreference = "Stop"

$repoPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $repoPath "tools"
$logPath = Join-Path $logDir "daily_git_backup.log"

if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

function Write-Log {
    param([string]$Message)
    $stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $logPath -Value "[$stamp] $Message"
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Args
    )

    & git @Args
    if ($LASTEXITCODE -ne 0) {
        throw ("git " + ($Args -join " ") + " failed with exit code " + $LASTEXITCODE)
    }
}

try {
    Set-Location $repoPath

    Invoke-Git -Args @("rev-parse", "--is-inside-work-tree")

    Invoke-Git -Args @("add", "-A")

    & git diff --cached --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Log "No changes. Skipping commit/push."
        exit 0
    }
    if ($LASTEXITCODE -ne 1) {
        throw "git diff --cached --quiet failed unexpectedly."
    }

    $msg = "chore: daily backup " + (Get-Date -Format "yyyy-MM-dd HH:mm")
    Invoke-Git -Args @("commit", "-m", $msg)

    Invoke-Git -Args @("push", "origin", "main")
    Write-Log "Backup completed successfully."
    exit 0
}
catch {
    Write-Log ("Backup failed: " + $_.Exception.Message)
    exit 1
}
