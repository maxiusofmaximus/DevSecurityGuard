# DevSecurityGuard Windows Service Installer

## PowerShell Installation Script

```powershell
# install-service.ps1
# Run as Administrator

$ErrorActionPreference = "Stop"

Write-Host "DevSecurityGuard Service Installer" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    exit 1
}

# Configuration
$ServiceName = "DevSecurityGuard"
$DisplayName = "DevSecurityGuard - Developer Security Service"
$Description = "Protects developers from npm/package manager malware and credential theft"
$BinaryPath = "$PSScriptRoot\DevSecurityGuard.Service.exe"

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Verify binary exists
if (-not (Test-Path $BinaryPath)) {
    Write-Host "ERROR: Service executable not found at: $BinaryPath" -ForegroundColor Red
    Write-Host "Please build the project in Release mode first:" -ForegroundColor Yellow
    Write-Host "  dotnet publish -c Release" -ForegroundColor Yellow
    exit 1
}

# Create service
Write-Host "Creating Windows Service..." -ForegroundColor Green
sc.exe create $ServiceName binPath= $BinaryPath start= auto DisplayName= $DisplayName

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create service" -ForegroundColor Red
    exit 1
}

# Set service description
sc.exe description $ServiceName $Description

# Create required directories
$dataDir = "C:\ProgramData\DevSecurityGuard"
if (-not (Test-Path $dataDir)) {
    Write-Host "Creating data directory: $dataDir" -ForegroundColor Green
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
}

# Start service
Write-Host "Starting service..." -ForegroundColor Green
Start-Service -Name $ServiceName

# Verify service is running
Start-Sleep -Seconds 2
$service = Get-Service -Name $ServiceName

if ($service.Status -eq "Running") {
    Write-Host ""
    Write-Host "SUCCESS: DevSecurityGuard service installed and running!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Service Status:" -ForegroundColor Cyan
    Write-Host "  Name: $($service.Name)"
    Write-Host "  Display Name: $($service.DisplayName)"
    Write-Host "  Status: $($service.Status)"
    Write-Host "  Start Type: $($service.StartType)"
    Write-Host ""
    Write-Host "Database Location: $dataDir\devsecurity.db" -ForegroundColor Cyan
    Write-Host "Logs Location: .\logs\" -ForegroundColor Cyan
} else {
    Write-Host "WARNING: Service created but not running. Status: $($service.Status)" -ForegroundColor Yellow
    Write-Host "Check logs for errors" -ForegroundColor Yellow
}
```

## PowerShell Uninstall Script

```powershell
# uninstall-service.ps1
# Run as Administrator

$ErrorActionPreference = "Stop"

Write-Host "DevSecurityGuard Service Uninstaller" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    exit 1
}

$ServiceName = "DevSecurityGuard"

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "Service not found. Nothing to uninstall." -ForegroundColor Yellow
    exit 0
}

# Stop service if running
if ($service.Status -eq "Running") {
    Write-Host "Stopping service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# Delete service
Write-Host "Removing service..." -ForegroundColor Green
sc.exe delete $ServiceName

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SUCCESS: DevSecurityGuard service removed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Note: Database and logs were NOT deleted." -ForegroundColor Yellow
    Write-Host "To remove data manually:" -ForegroundColor Yellow
    Write-Host "  rd /s C:\ProgramData\DevSecurityGuard" -ForegroundColor Gray
    Write-Host "  rd /s .\logs" -ForegroundColor Gray
} else {
    Write-Host "ERROR: Failed to remove service" -ForegroundColor Red
    exit 1
}
```

## Usage

### Installation

1. Build the project in Release mode:
   ```powershell
   dotnet publish -c Release
   ```

2. Navigate to the output directory:
   ```powershell
   cd bin\Release\net8.0\publish
   ```

3. Run installer as Administrator:
   ```powershell
   .\install-service.ps1
   ```

### Verification

Check service status:
```powershell
Get-Service DevSecurityGuard
sc query DevSecurityGuard
```

View logs:
```powershell
Get-Content logs\devsecurityguard-*.log -Tail 50
```

### Uninstallation

Run uninstaller as Administrator:
```powershell
.\uninstall-service.ps1
```

## Manual Installation (Alternative)

```powershell
# Create service
sc create DevSecurityGuard binPath="<path-to-exe>" start=auto

# Set description
sc description DevSecurityGuard "Developer Security Service"

# Start service
sc start DevSecurityGuard

# Delete service (when uninstalling)
sc delete DevSecurityGuard
```

## Troubleshooting

### Service won't start
- Check logs in `logs\` directory
- Verify .NET 8 Runtime is installed
- Ensure database directory exists: `C:\ProgramData\DevSecurityGuard`

### Permission errors
- Ensure running PowerShell as Administrator
- Check Windows Event Viewer (Application log)

### Service disappears after reboot
- Verify Start Type is set to "Automatic"
- Check Windows Services (services.msc)
