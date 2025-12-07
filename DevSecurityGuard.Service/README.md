# DevSecurityGuard Service

Windows service that monitors npm/yarn/pnpm package installations for malware and security threats.

## Components

- **DetectionEngines/**: Threat detection modules (typosquatting, malicious scripts, etc.)
- **Database/**: Entity Framework Core DbContext for SQLite
- **Models/**: Data models, enums, and DTOs
- **ThreatIntelligence/**: External API integrations (planned)

## Running Locally

```powershell
dotnet run
```

## Building

```powershell
dotnet build
```

## Installing as Windows Service

```powershell
# Publish for Windows
dotnet publish -c Release -r win-x64 --self-contained

# Install service  
sc create DevSecurityGuard binPath="<path-to-published-exe>"
sc start DevSecurityGuard
```

## Configuration

All configuration is stored in SQLite database at:
```
C:\ProgramData\DevSecurityGuard\devsecurity.db
```

Default settings are seeded on first run.
