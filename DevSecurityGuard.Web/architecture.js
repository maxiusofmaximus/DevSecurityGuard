// Architecture visualization and monitoring
const API_BASE_URL = 'http://localhost:5000/api';
const SIGNALR_HUB_URL = 'http://localhost:5000/hubs/devsecurity';

let connection = null;
let componentStatus = {
    api: false,
    database: false,
    webUI: true, // Always true if page is loaded
    desktopUI: false,
    service: false,
    detectors: {
        shaiHulud: false,
        credentials: false,
        typosquatting: false,
        maliciousScripts: false,
        supplyChain: false
    }
};

// Initialize Mermaid
mermaid.initialize({
    startOnLoad: false,
    theme: 'dark',
    themeVariables: {
        primaryColor: '#0E639C',
        primaryTextColor: '#fff',
        primaryBorderColor: '#4EC9B0',
        lineColor: '#4EC9B0',
        secondaryColor: '#2D2D30',
        tertiaryColor: '#3E3E42'
    }
});

// Generate main architecture diagram
function generateArchitectureDiagram() {
    const apiStatus = componentStatus.api ? ':::online' : ':::offline';
    const dbStatus = componentStatus.database ? ':::online' : ':::offline';
    const serviceStatus = componentStatus.service ? ':::online' : ':::offline';

    return `
graph TB
    subgraph Client["${tArch('webUI')} / ${tArch('desktopUI')}"]
        WebUI["🌐 ${tArch('webUI')}<br/>${tArch('webUIDesc')}"]:::online
        DesktopUI["🖥️ ${tArch('desktopUI')}<br/>${tArch('desktopUIDesc')}"]
    end
    
    subgraph Backend["${tArch('apiBackend')}"]
        API["🔌 REST API<br/>${tArch('apiDesc')}"]${apiStatus}
        SignalR["📡 ${tArch('signalrHub')}<br/>${tArch('hubDesc')}"]${apiStatus}
    end
    
    subgraph Data["${tArch('database')}"]
        DB["💾 SQLite<br/>${tArch('dbDesc')}"]${dbStatus}
    end
    
    subgraph Service["${tArch('windowsService')}"]
        WinService["⚙️ Background Service<br/>${tArch('serviceDesc')}"]${serviceStatus}
        Detectors["🔍 ${tArch('componentsTitle')}"]${serviceStatus}
    end
    
    WebUI <-->|"${tArch('httpRequests')}"| API
    DesktopUI <-->|"${tArch('httpRequests')}"| API
    WebUI <-.->|"${tArch('realTimeUpdates')}"| SignalR
    DesktopUI <-.->|"${tArch('realTimeUpdates')}"| SignalR
    
    API <-->|"${tArch('databaseAccess')}"| DB
    WinService <-->|"${tArch('databaseAccess')}"| DB
    
    WinService -.->|"${tArch('threatDetection')}"| API
    Detectors -.->|"${tArch('packageAnalysis')}"| WinService
    
    classDef online fill:#1a5f3f,stroke:#4EC9B0,stroke-width:2px,color:#fff
    classDef offline fill:#5f1a1a,stroke:#F14C4C,stroke-width:2px,color:#fff
    `;
}

// Generate data flow diagram
function generateDataFlowDiagram() {
    return `
sequenceDiagram
    participant U as User
    participant W as Web/Desktop UI
    participant A as API
    participant S as SignalR
    participant D as Database
    participant WS as Windows Service
    
    U->>W: ${tArch('httpRequests').split(' ')[0]}
    W->>A: GET/PUT /api/config
    A->>D: Query/Update
    D-->>A: Response
    A-->>W: JSON Response
    A->>S: Notify Change
    S-->>W: Push Update
    
    WS->>D: Log Threat
    WS->>A: POST /api/activity
    A->>S: Broadcast Event
    S-->>W: Real-time Alert
    `;
}

// Check API status
async function checkAPIStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/activity/stats`, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });

        if (response.ok) {
            componentStatus.api = true;
            componentStatus.database = true; // If API works, DB is accessible
            return true;
        }
    } catch (error) {
        componentStatus.api = false;
        componentStatus.database = false;
    }
    return false;
}

// Connect to SignalR
async function connectSignalR() {
    try {
        connection = new signalR.HubConnectionBuilder()
            .withUrl(SIGNALR_HUB_URL)
            .withAutomaticReconnect()
            .build();

        connection.on('ConfigUpdated', () => {
            updateStatus();
        });

        connection.on('ActivityUpdated', () => {
            updateStatus();
        });

        await connection.start();
        componentStatus.service = true;
        return true;
    } catch (error) {
        console.error('SignalR connection failed:', error);
        componentStatus.service = false;
        return false;
    }
}

// Update component status display
function updateStatusGrid() {
    const grid = document.getElementById('status-grid');
    grid.innerHTML = '';

    const components = [
        { name: tArch('webUI'), status: true, detail: tArch('webUIDesc') },
        { name: tArch('apiBackend'), status: componentStatus.api, detail: 'localhost:5000' },
        { name: tArch('database'), status: componentStatus.database, detail: 'devsecurity.db' },
        { name: tArch('signalrHub'), status: componentStatus.service, detail: tArch('realTimeUpdates') },
        { name: tArch('windowsService'), status: componentStatus.service, detail: tArch('serviceDesc') }
    ];

    components.forEach(comp => {
        const card = document.createElement('div');
        card.className = 'status-card';

        const statusClass = comp.status ? 'status-online' : 'status-offline';
        const statusText = comp.status ? tArch('online') : tArch('offline');

        card.innerHTML = `
            <div class="status-indicator">
                <div class="status-dot ${statusClass}"></div>
                <span class="status-label">${comp.name}</span>
            </div>
            <div class="status-detail">${comp.detail}</div>
            <div class="status-detail">${statusText}</div>
        `;

        grid.appendChild(card);
    });
}

// Update component list
function updateComponentList() {
    const list = document.getElementById('component-list');
    list.innerHTML = '';

    const detectors = [
        { name: tArch('shaiHuludDetector'), key: 'shaiHulud', priority: 100 },
        { name: tArch('credentialDetector'), key: 'credentials', priority: 95 },
        { name: tArch('typosquattingDetector'), key: 'typosquatting', priority: 90 },
        { name: tArch('maliciousScriptDetector'), key: 'maliciousScripts', priority: 85 },
        { name: tArch('supplyChainDetector'), key: 'supplyChain', priority: 80 }
    ];

    detectors.forEach(det => {
        const li = document.createElement('li');
        li.className = 'component-item';

        const status = componentStatus.service ? tArch('active') : tArch('inactive');
        const statusClass = componentStatus.service ? 'status-active' : 'status-inactive';

        li.innerHTML = `
            <span class="component-name">${det.name} (Priority: ${det.priority})</span>
            <span class="component-status ${statusClass}">${status}</span>
        `;

        list.appendChild(li);
    });
}

// Regenerate all diagrams
async function regenerateDiagrams() {
    // Main architecture diagram
    const archDiagram = document.getElementById('architecture-diagram');
    archDiagram.innerHTML = generateArchitectureDiagram();
    await mermaid.run({ nodes: [archDiagram] });

    // Data flow diagram
    const flowDiagram = document.getElementById('dataflow-diagram');
    flowDiagram.innerHTML = generateDataFlowDiagram();
    await mermaid.run({ nodes: [flowDiagram] });
}

// Full status update
async function updateStatus() {
    await checkAPIStatus();
    updateStatusGrid();
    updateComponentList();
    await regenerateDiagrams();

    // Update timestamp
    const now = new Date().toLocaleTimeString();
    document.getElementById('update-time').textContent = now;
}

// Initialize page
document.addEventListener('DOMContentLoaded', async () => {
    document.getElementById('update-time').textContent = tArch('checking');

    // Initial status check
    await updateStatus();

    // Try to connect SignalR
    await connectSignalR();
    await updateStatus();

    // Auto-refresh every 10 seconds
    setInterval(updateStatus, 10000);

    console.log('Architecture page initialized');
});
