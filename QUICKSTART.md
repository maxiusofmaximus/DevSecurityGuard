# DevSecurityGuard - Quick Start Guide

## 🚀 System Components

Your DevSecurityGuard installation includes:

| Component | Status | Description |
|-----------|--------|-------------|
| **DevSecurityGuard.Core** | ✅ Ready | 8 package managers (npm, pip, cargo, nuget, maven, gradle, gem, composer) |
| **DevSecurityGuard.PluginSystem** | ✅ Ready | Dynamic plugin loading with hot-reload |
| **DevSecurityGuard.Service** | ✅ Ready | Windows background service |
| **DevSecurityGuard.API** | ✅ Ready | REST API + SignalR (port 5000) |
| **DevSecurityGuard.Web** | ✅ Ready | Browser-based UI |
| **DevSecurityGuard.UI** | ✅ Ready | WPF Desktop application |

---

## 🎯 Quick Start (3 Options)

### Option 1: Complete System Demo (Recommended)

```batch
START-SYSTEM.bat
```

This will:
1. ✅ Build all projects (Release mode)
2. ✅ Start API server (localhost:5000)
3. ✅ Open Web UI in browser
4. ✅ Open Architecture visualization
5. ✅ Show system status

**What to expect:**
- API console window will open
- Web UI will load in your browser
- Architecture page shows all components

---

### Option 2: API + Web UI Only

```batch
start-api.bat   # Terminal 1: Start API
```

Then open in browser:
- Web UI: `file:///path/to/DevSecurityGuard.Web/index.html`
- Architecture: `file:///path/to/DevSecurityGuard.Web/architecture.html`

---

### Option 3: Desktop UI

```batch
cd DevSecurityGuard.UI
dotnet run
```

Opens the WPF desktop application.

---

## 🧪 Testing

### Run All Tests

```batch
RUN-TESTS.bat
```

### Run Specific Tests

```batch
cd DevSecurityGuard.Tests
dotnet test --filter "FullyQualifiedName~Typosquatting"
```

---

## 🔍 What You Can Test

### 1. Multi-Package Manager Detection

The system can now detect and analyze 8 package managers:

```
Your Project/
├── package.json      → npm detected ✅
├── requirements.txt  → pip detected ✅
├── Cargo.toml        → cargo detected ✅
├── *.csproj          → nuget detected ✅
├── pom.xml           → maven detected ✅
├── build.gradle      → gradle detected ✅
├── Gemfile           → gem detected ✅
└── composer.json     → composer detected ✅
```

### 2. Plugin System

Plugins can be loaded dynamically:

```csharp
var registry = new PluginRegistry("~/.devsecurityguard/plugins");
await registry.InitializeAsync();
await registry.LoadAllPluginsAsync();
```

Example plugin structure:
```
plugins/
└── my-detector/
    ├── plugin.json
    └── MyDetector.dll
```

### 3. Web UI Features

Open `DevSecurityGuard.Web/index.html`:

- 📊 **Dashboard:** Real-time statistics
- 🔔 **Activity Log:** Recent threats and scans
- ⚙️ **Settings:** 
  - Intervention mode (Automatic/Interactive/Alert)
  - Force pnpm enforcement
  - .env file protection
  - Credential monitoring
- 🌐 **Language:** English/Spanish switcher
- 🔄 **Real-time Updates:** SignalR integration

### 4. Architecture Visualization

Open `DevSecurityGuard.Web/architecture.html`:

- 🏗️ **Mermaid Diagrams:** System architecture
- 🔴🟢 **Live Status:** Component health checks
- 🌐 **Multi-language:** ES/EN support
- 🔄 **Auto-refresh:** Every 10 seconds

### 5. API Endpoints

API running on `http://localhost:5000`:

```bash
# Get configuration
GET /api/config

# Update configuration
PUT /api/config
{
  "key": "InterventionMode",
  "value": "interactive"
}

# Get recent activity
GET /api/activity

# Get statistics
GET /api/activity/stats

# SignalR Hub
ws://localhost:5000/hubs/devsecurity
```

---

## 📁 Project Structure

```
SubsystemDeveloper/
├── DevSecurityGuard.Core/          # 8 Package Managers
│   ├── Abstractions/
│   │   ├── IPackageManager.cs
│   │   └── IPackageManagerFactory.cs
│   └── PackageManagers/
│       ├── NpmPackageManager.cs
│       ├── PipPackageManager.cs
│       ├── CargoPackageManager.cs
│       ├── NuGetPackageManager.cs
│       ├── MavenPackageManager.cs
│       ├── GradlePackageManager.cs
│       ├── GemPackageManager.cs
│       └── ComposerPackageManager.cs
│
├── DevSecurityGuard.PluginSystem/  # Plugin Infrastructure
│   ├── IPlugin.cs
│   ├── IDetectorPlugin.cs
│   ├── PluginManifest.cs
│   ├── PluginLoader.cs
│   └── PluginRegistry.cs
│
├── DevSecurityGuard.Service/       # Windows Service
│   ├── DetectionEngines/           # 5 Threat Detectors
│   ├── PackageManagerInterceptor.cs
│   └── ProcessMonitor.cs
│
├── DevSecurityGuard.API/           # REST API + SignalR
│   ├── Controllers/
│   ├── Hubs/
│   └── Program.cs
│
├── DevSecurityGuard.Web/           # Browser UI
│   ├── index.html
│   ├── architecture.html
│   ├── app.js
│   ├── api-client.js
│   └── translations.js
│
├── DevSecurityGuard.UI/            # WPF Desktop
│   ├── MainWindow.xaml
│   └── LocalizationManager.cs
│
└── DevSecurityGuard.Tests/         # Unit Tests
    ├── TyposquattingDetectorTests.cs
    └── PackageManagerInterceptorTests.cs
```

---

## 🎯 Key Features Implemented

### ✅ Phase 1: Multi-Package Manager Support
- 8 package managers with full abstraction
- Registry API integration for all
- Auto-detection of project types
- ~2,500 lines of code

### ✅ Phase 2: Plugin-Based Architecture
- Dynamic plugin loading (AssemblyLoadContext)
- Hot-reload/hot-unload support
- Priority-based execution
- JSON manifest system
- ~600 lines of code

### ✅ Previous: Core Features
- 5 threat detectors (Typosquatting, Credential Theft, Shai-Hulud, Malicious Scripts, Supply Chain)
- Multi-language support (EN/ES)
- Real-time UI synchronization (SignalR)
- Architecture visualization
- REST API with Swagger

**Total:** ~10,000+ lines of production code

---

## 🐛 Troubleshooting

### API Won't Start

```batch
# Check if port 5000 is in use
netstat -ano | findstr :5000

# Kill process if needed
taskkill /PID <PID> /F
```

### Web UI Not Connecting to API

1. Ensure API is running (check console)
2. Check browser console for errors
3. Verify CORS is enabled in API
4. Try: `http://localhost:5000/api/config`

### Desktop UI Won't Open

```batch
# Rebuild in Debug mode
cd DevSecurityGuard.UI
dotnet build
dotnet run
```

---

## 📊 System Requirements

- **.NET 8 SDK** (required)
- **Windows 10/11** (for Service and Desktop UI)
- **Modern Browser** (for Web UI - Chrome, Firefox, Edge)
- **~100MB Disk Space**
- **~200MB RAM** (when running)

---

## 🎓 Next Steps

After testing, you can:

1. **Install as Windows Service:**
   ```powershell
   .\scripts\install-service.ps1
   ```

2. **Create Custom Plugins:**
   See `examples/plugins/example-detector/`

3. **Integrate with CI/CD:**
   Use REST API endpoints in your pipelines

4. **Extend Package Managers:**
   Add Homebrew, APT, or other PMs as plugins

---

## 📚 Documentation

- **Implementation Plan:** `brain/implementation_plan.md`
- **Walkthrough:** `brain/walkthrough.md`
- **Phase 2 Summary:** `brain/phase2_summary.md`
- **Task List:** `brain/task.md`

---

## ✅ Verification Checklist

Before reporting issues, verify:

- [ ] .NET 8 SDK installed (`dotnet --version`)
- [ ] All projects build (`dotnet build`)
- [ ] Tests pass (`dotnet test`)
- [ ] API starts (`start-api.bat`)
- [ ] Web UI loads (check browser console)
- [ ] No firewall blocking port 5000

---

**Ready to test? Run `START-SYSTEM.bat` and explore!** 🚀
