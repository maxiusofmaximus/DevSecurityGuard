# DevSecurityGuard Installer
# This script installs the DevSecurityGuard Service and configures the system.

$ServiceName = "DevSecurityGuard"
$ServiceDisplayName = "DevSecurityGuard Protection Service"
$BinPath = Join-Path $PSScriptRoot "DevSecurityGuard.Service\bin\Release\net8.0\DevSecurityGuard.Service.exe"

Write-Host "🛡️  Installing DevSecurityGuard..." -ForegroundColor Cyan

# Check for Administrator privileges
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

# Build the solution first
Write-Host "🔨 Building solution..." -ForegroundColor Gray
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Aborting installation."
    exit 1
}

# Stop existing service if running
if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service $ServiceName -Force
    
    Write-Host "Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Register the service (using sc.exe for reliability with .NET worker services)
Write-Host "📝 Registering Windows Service..." -ForegroundColor Gray
$BinPath = """$BinPath""" # Quote path
sc.exe create $ServiceName binPath= $BinPath start= auto DisplayName= "$ServiceDisplayName"

if ($LASTEXITCODE -eq 0) {
    # Set description
    sc.exe description $ServiceName "Real-time protection for developer environments against supply chain attacks."
    
    # Configure recovery options
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

    Write-Host "🚀 Starting service..." -ForegroundColor Gray
    Start-Service $ServiceName

    Write-Host "✅ Installation Complete!" -ForegroundColor Green
    Write-Host "DevSecurityGuard is now protecting your system."
} else {
    Write-Error "Failed to install service."
}

# Create Start Menu shortcut for Launcher
$ShortcutPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\DevSecurityGuard.lnk"
$LauncherPath = Join-Path $PSScriptRoot "DevSecurityGuard.Launcher\bin\Release\net8.0-windows\DevSecurityGuard.Launcher.exe"

if (Test-Path $LauncherPath) {
    $WshShell = New-Object -comObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $LauncherPath
    $Shortcut.Save()
    Write-Host "Created Start Menu shortcut." -ForegroundColor Gray
}
