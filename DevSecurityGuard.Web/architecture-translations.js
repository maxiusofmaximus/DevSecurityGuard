// Translations for architecture page
const archTranslations = {
    en: {
        pageTitle: 'DevSecurityGuard Architecture',
        statusTitle: 'System Status',
        diagramTitle: 'Architecture Overview',
        componentsTitle: 'Components',
        dataflowTitle: 'Data Flow',
        lastUpdated: 'Last updated',

        // Component names
        webUI: 'Web UI',
        desktopUI: 'Desktop UI',
        apiBackend: 'API Backend',
        database: 'SQLite Database',
        windowsService: 'Windows Service',
        signalrHub: 'SignalR Hub',

        // Detector names
        shaiHuludDetector: 'Shai-Hulud Detector',
        credentialDetector: 'Credential Theft Detector',
        typosquattingDetector: 'Typosquatting Detector',
        maliciousScriptDetector: 'Malicious Script Detector',
        supplyChainDetector: 'Supply Chain Attack Detector',

        // Status
        online: 'Online',
        offline: 'Offline',
        active: 'Active',
        inactive: 'Inactive',
        checking: 'Checking...',

        // Descriptions
        webUIDesc: 'Browser-based interface',
        desktopUIDesc: 'WPF Windows application',
        apiDesc: 'REST API + SignalR',
        dbDesc: 'Configuration & threats storage',
        serviceDesc: 'Background protection service',
        hubDesc: 'Real-time updates',

        // Diagram labels
        httpRequests: 'HTTP Requests',
        realTimeUpdates: 'Real-time Updates',
        databaseAccess: 'Database Access',
        threatDetection: 'Threat Detection',
        packageAnalysis: 'Package Analysis'
    },
    es: {
        pageTitle: 'Arquitectura de DevSecurityGuard',
        statusTitle: 'Estado del Sistema',
        diagramTitle: 'Vista General de Arquitectura',
        componentsTitle: 'Componentes',
        dataflowTitle: 'Flujo de Datos',
        lastUpdated: 'Última actualización',

        // Component names
        webUI: 'UI Web',
        desktopUI: 'UI Escritorio',
        apiBackend: 'API Backend',
        database: 'Base de Datos SQLite',
        windowsService: 'Servicio Windows',
        signalrHub: 'Hub SignalR',

        // Detector names
        shaiHuludDetector: 'Detector Shai-Hulud',
        credentialDetector: 'Detector Robo Credenciales',
        typosquattingDetector: 'Detector Typosquatting',
        maliciousScriptDetector: 'Detector Scripts Maliciosos',
        supplyChainDetector: 'Detector Ataques Cadena Suministro',

        // Status
        online: 'En línea',
        offline: 'Desconectado',
        active: 'Activo',
        inactive: 'Inactivo',
        checking: 'Verificando...',

        // Descriptions
        webUIDesc: 'Interfaz basada en navegador',
        desktopUIDesc: 'Aplicación WPF Windows',
        apiDesc: 'API REST + SignalR',
        dbDesc: 'Almacén de configuración y amenazas',
        serviceDesc: 'Servicio de protección en segundo plano',
        hubDesc: 'Actualizaciones en tiempo real',

        // Diagram labels
        httpRequests: 'Peticiones HTTP',
        realTimeUpdates: 'Actualizaciones Tiempo Real',
        databaseAccess: 'Acceso Base de Datos',
        threatDetection: 'Detección de Amenazas',
        packageAnalysis: 'Análisis de Paquetes'
    }
};

let currentLang = localStorage.getItem('archLanguage') || 'en';

function tArch(key) {
    return archTranslations[currentLang][key] || key;
}

function changeLanguage(lang) {
    currentLang = lang;
    localStorage.setItem('archLanguage', lang);
    updatePageLanguage();
    regenerateDiagrams();
}

function updatePageLanguage() {
    document.getElementById('page-title').textContent = tArch('pageTitle');
    document.getElementById('status-title').textContent = tArch('statusTitle');
    document.getElementById('diagram-title').textContent = tArch('diagramTitle');
    document.getElementById('components-title').textContent = tArch('componentsTitle');
    document.getElementById('dataflow-title').textContent = tArch('dataflowTitle');

    const lastUpdateText = document.querySelector('#last-update');
    if (lastUpdateText) {
        const timeSpan = document.getElementById('update-time');
        const time = timeSpan.textContent;
        lastUpdateText.innerHTML = `${tArch('lastUpdated')}: <span id="update-time">${time}</span>`;
    }
}

// Initialize language on load
document.addEventListener('DOMContentLoaded', () => {
    const selector = document.getElementById('language-selector');
    if (selector) {
        selector.value = currentLang;
    }
    updatePageLanguage();
});
