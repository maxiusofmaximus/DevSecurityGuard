# DevSecurityGuard Uninstaller

$ServiceName = "DevSecurityGuard"

Write-Host "🗑️  Uninstalling DevSecurityGuard..." -ForegroundColor Cyan

# Check for Administrator privileges
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping service..." -ForegroundColor Yellow
    Stop-Service $ServiceName -Force -ErrorAction SilentlyContinue
    
    Write-Host "Removing service registration..." -ForegroundColor Yellow
    sc.exe delete $ServiceName
    
    Write-Host "✅ Service uninstalled successfully." -ForegroundColor Green
} else {
    Write-Warning "Service '$ServiceName' is not installed."
}

# Remove Shortcut
$ShortcutPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\DevSecurityGuard.lnk"
if (Test-Path $ShortcutPath) {
    Remove-Item $ShortcutPath -Force
    Write-Host "Removed Start Menu shortcut." -ForegroundColor Gray
}

Write-Host "Uninstallation complete."
