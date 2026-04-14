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

try {
    Set-Location $repoPath

    git rev-parse --is-inside-work-tree | Out-Null

    git add -A

    git diff --cached --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Log "No changes. Skipping commit/push."
        exit 0
    }

    $msg = "chore: daily backup " + (Get-Date -Format "yyyy-MM-dd HH:mm")
    git commit -m $msg | Out-Null

    git push origin main | Out-Null
    Write-Log "Backup completed successfully."
    exit 0
}
catch {
    Write-Log ("Backup failed: " + $_.Exception.Message)
    exit 1
}
