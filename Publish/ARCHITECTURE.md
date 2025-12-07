# System Architecture - DevSecurityGuard

## Overview
DevSecurityGuard is a modular, real-time security subsystem designed to protect developer environments from supply chain attacks. It operates as a background Windows Service with multiple user interfaces.

## High-Level Architecture

```mermaid
graph TD
    User[User] --> Launcher[Hybrid Launcher]
    Launcher --> |Starts| WebUI[Web Dashboard]
    Launcher --> |Starts| DesktopUI[Desktop App (WPF)]
    Launcher --> |Starts| CLI[Command Line Interface]
    
    WebUI -.-> |IPC/HTTP| Service[Windows Service (Core)]
    DesktopUI -.-> |IPC/NamedPipes| Service
    CLI -.-> |IPC/NamedPipes| Service
    
    subgraph "Privileged Core (System Context)"
        Service --> |Load| Detectors[Detection Engines]
        Service --> |Monitor| FileSystem[File System Watcher]
        Service --> |Intercept| ProcessMonitor[Process Monitor]
        
        Detectors --> Typo[Typosquatting Engine]
        Detectors --> Script[Malicious Script Analyzer]
        Detectors --> Creds[Credential Theft Scanner]
        Detectors --> Supply[Supply Chain Validator]
        
        Service --> |Store| DB[(Local SQLite DB)]
    end
    
    subgraph "External World"
        Service --> |Fetch| ThreatFeed[Threat Intelligence APIs]
        ProcessMonitor --> |Intercepts| NPM[npm/yarn/pnpm]
        ProcessMonitor --> |Intercepts| PIP[pip/python]
    end
```

## Component Breakdown

### 1. Presentation Layer (UI)
- **Launcher (`DevSecurityGuard.Launcher`)**: 
  - Entry point.
  - Hybrid "Gaming/Win11" design.
  - Responsibilities: Routing user to Web or Desktop UI, ensuring Service is running.
- **Desktop UI (`DevSecurityGuard.UI`)**:
  - Native WPF application.
  - System Tray integration.
  - Settings management.
  - Real-time notifications (Toasts).
- **Web UI (`DevSecurityGuard.Web`)**:
  - Modern HTML5/JS dashboard.
  - Visualizes complex threat graphs.
- **CLI (`DevSecurityGuard.CLI`)**:
  - Headless operation.
  - CI/CD integration support.

### 2. Service Layer (Core)
- **Windows Service (`DevSecurityGuard.Service`)**:
  - Runs as `LOCALSYSTEM` for maximum privilege.
  - Manages lifecycle of protection engines.
  - Handles IPC requests from UIs.
- **Database**:
  - SQLite local database for logs, configuration, and cache.
  - Zero-knowledge encryption ready.

### 3. Detection Layer (The Brain)
- **`IThreatDetector` Interface**: Standard contract for all engines.
- **Implemented Engines**:
  - `TyposquattingDetector`: Levenshtein distance & popularity checks.
  - `CredentialTheftDetector`: Regex & entropy analysis for secrets.
  - `MaliciousScriptDetector`: Heuristic analysis of `package.json` scripts.
  - `SupplyChainDetector`: Metadata validation against registries.

### 4. Interception Layer
- **Process Monitor**: Hooks into shell execution to detect package manager commands.
- **File Watcher**: Real-time monitoring of sensitive files (`.env`, `package.json`).

## Security & Privacy
- **Local-First**: No data leaves the machine unless "Community Feed" is explicitly enabled.
- **Privilege Separation**: UI runs as User, Service runs as System.
