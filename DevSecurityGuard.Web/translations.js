// Language translations
const translations = {
    en: {
        // Top Bar
        appTitle: 'DevSecurityGuard',
        statusActive: 'ACTIVE',
        statusUpdating: 'UPDATING...',
        statusRestarting: 'RESTARTING',
        version: 'v1.0.0',
        settingsBtn: '⚙️ Settings',

        // Sections
        protectionOverview: 'Protection Overview',
        recentActivity: 'Recent Activity',
        quickSettings: 'Quick Settings',
        serviceControl: 'Service Control',
        detectionEngines: 'Detection Engines',

        // Statistics
        threatsBlocked: 'Threats Blocked',
        packagesScanned: 'Packages Scanned',
        activeDetectors: 'Active Detectors',

        // Settings
        interventionMode: 'Intervention Mode',
        modeAutomatic: '🛑 Automatic',
        modeInteractive: '💬 Interactive',
        modeAlert: '🔔 Alert Only',
        forcePnpm: 'Force pnpm (redirect npm/yarn)',
        protectEnv: 'Protect .env files',
        monitorCredentials: 'Monitor credentials (SSH, cloud)',

        // Buttons
        restartService: '🔄 Restart Service',
        restarting: '⏳ Restarting...',
        viewLogs: '📄 View Logs',

        // Detectors
        detectorShaiHulud: '🔴 Shai-Hulud',
        detectorCredentials: '🔑 Credential Theft',
        detectorTyposquatting: '📝 Typosquatting',
        detectorMaliciousScripts: '💻 Malicious Scripts',
        detectorSupplyChain: '🔗 Supply Chain',
        priority: 'Priority',

        // Activity Messages
        serviceStarted: '✅ Service started successfully',
        analyzedPackage: '📦 Analyzed package',
        blockedTyposquatting: '🚫 Blocked typosquatting',
        warningNewPackage: '⚠️ Warning: Package published < 24h ago',
        detectorsLoaded: '✅ All detectors loaded successfully',
        changedMode: '⚙️ Changed intervention mode to',
        enabled: 'ENABLED',
        disabled: 'DISABLED',
        serviceRestarted: '🔄 Service restarted successfully',
        openingLogs: '📄 Opening logs directory...',

        // Times
        justNow: 'Just now',
        minutesAgo: 'minutes ago',

        // Dialogs
        restartConfirm: 'Are you sure you want to restart the DevSecurityGuard service?\n\nProtection will be temporarily unavailable during the restart.',
        advancedSettings: 'Advanced settings configuration coming soon!\n\nCurrent settings are available in the Quick Settings panel.',
        logsInfo: 'Logs would open here.\n\nIn the desktop version, this opens the logs folder.\nIn the web version, this could download logs or show them in a modal.',

        // Language Selector
        language: 'Language',
        languageEnglish: '🇺🇸 English',
        languageSpanish: '🇪🇸 Español'
    },
    es: {
        // Top Bar
        appTitle: 'DevSecurityGuard',
        statusActive: 'ACTIVO',
        statusUpdating: 'ACTUALIZANDO...',
        statusRestarting: 'REINICIANDO',
        version: 'v1.0.0',
        settingsBtn: '⚙️ Configuración',

        // Sections
        protectionOverview: 'Resumen de Protección',
        recentActivity: 'Actividad Reciente',
        quickSettings: 'Configuración Rápida',
        serviceControl: 'Control del Servicio',
        detectionEngines: 'Motores de Detección',

        // Statistics
        threatsBlocked: 'Amenazas Bloqueadas',
        packagesScanned: 'Paquetes Escaneados',
        activeDetectors: 'Detectores Activos',

        // Settings
        interventionMode: 'Modo de Intervención',
        modeAutomatic: '🛑 Automático',
        modeInteractive: '💬 Interactivo',
        modeAlert: '🔔 Solo Alertas',
        forcePnpm: 'Forzar pnpm (redirigir npm/yarn)',
        protectEnv: 'Proteger archivos .env',
        monitorCredentials: 'Monitorear credenciales (SSH, nube)',

        // Buttons
        restartService: '🔄 Reiniciar Servicio',
        restarting: '⏳ Reiniciando...',
        viewLogs: '📄 Ver Registros',

        // Detectors
        detectorShaiHulud: '🔴 Shai-Hulud',
        detectorCredentials: '🔑 Robo de Credenciales',
        detectorTyposquatting: '📝 Typosquatting',
        detectorMaliciousScripts: '💻 Scripts Maliciosos',
        detectorSupplyChain: '🔗 Cadena de Suministro',
        priority: 'Prioridad',

        // Activity Messages
        serviceStarted: '✅ Servicio iniciado correctamente',
        analyzedPackage: '📦 Paquete analizado',
        blockedTyposquatting: '🚫 Typosquatting bloqueado',
        warningNewPackage: '⚠️ Advertencia: Paquete publicado hace < 24h',
        detectorsLoaded: '✅ Todos los detectores cargados correctamente',
        changedMode: '⚙️ Modo de intervención cambiado a',
        enabled: 'HABILITADO',
        disabled: 'DESHABILITADO',
        serviceRestarted: '🔄 Servicio reiniciado correctamente',
        openingLogs: '📄 Abriendo directorio de registros...',

        // Times
        justNow: 'Justo ahora',
        minutesAgo: 'minutos',

        // Dialogs
        restartConfirm: '¿Está seguro de que desea reiniciar el servicio DevSecurityGuard?\n\nLa protección no estará disponible temporalmente durante el reinicio.',
        advancedSettings: '¡Configuración avanzada próximamente!\n\nLa configuración actual está disponible en el panel de Configuración Rápida.',
        logsInfo: 'Los registros se abrirían aquí.\n\nEn la versión de escritorio, esto abre la carpeta de registros.\nEn la versión web, esto podría descargar los registros o mostrarlos en un modal.',

        // Language Selector
        language: 'Idioma',
        languageEnglish: '🇺🇸 English',
        languageSpanish: '🇪🇸 Español'
    }
};

// Current language
let currentLanguage = localStorage.getItem('appLanguage') || 'en';

// Get translation
function t(key) {
    return translations[currentLanguage][key] || key;
}

// Update all UI text
function updateUILanguage() {
    // Update text content elements
    const elements = {
        'section-title-protection': 'protectionOverview',
        'section-title-activity': 'recentActivity',
        'section-title-settings': 'quickSettings',
        'section-title-control': 'serviceControl',
        'section-title-detectors': 'detectionEngines',
        'stat-label-threats': 'threatsBlocked',
        'stat-label-packages': 'packagesScanned',
        'stat-label-detectors': 'activeDetectors',
        'label-intervention': 'interventionMode',
        'detector-name-shai': 'detectorShaiHulud',
        'detector-name-cred': 'detectorCredentials',
        'detector-name-typo': 'detectorTyposquatting',
        'detector-name-script': 'detectorMaliciousScripts',
        'detector-name-supply': 'detectorSupplyChain'
    };

    for (const [id, key] of Object.entries(elements)) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = t(key);
        }
    }

    // Update buttons
    document.querySelector('[onclick="openSettings()"]').textContent = t('settingsBtn');
    document.querySelector('[onclick="restartService()"]').textContent = t('restartService');
    document.querySelector('[onclick="viewLogs()"]').textContent = t('viewLogs');

    // Update select options
    const interventionSelect = document.getElementById('intervention-mode');
    if (interventionSelect) {
        interventionSelect.options[0].text = t('modeAutomatic');
        interventionSelect.options[1].text = t('modeInteractive');
        interventionSelect.options[2].text = t('modeAlert');
    }

    // Update checkboxes
    const checkboxLabels = document.querySelectorAll('.checkbox-label span');
    if (checkboxLabels[0]) checkboxLabels[0].textContent = t('forcePnpm');
    if (checkboxLabels[1]) checkboxLabels[1].textContent = t('protectEnv');
    if (checkboxLabels[2]) checkboxLabels[2].textContent = t('monitorCredentials');

    // Update priority labels
    document.querySelectorAll('.detector-priority').forEach(el => {
        const priorityNum = el.textContent.match(/\d+/)[0];
        el.textContent = `${t('priority')} ${priorityNum}`;
    });

    // Reload activity with translated messages
    reloadActivity();

    // Save language preference
    localStorage.setItem('appLanguage', currentLanguage);

    // Notify sync (if available)
    if (typeof notifyLanguageChange === 'function') {
        notifyLanguageChange(currentLanguage);
    }
}

// Change language
function changeLanguage(lang) {
    currentLanguage = lang;
    updateUILanguage();
    addActivityItem(
        `🌐 ${t('language')}: ${lang === 'es' ? 'Español' : 'English'}`,
        t('justNow'),
        'info'
    );
}

// Reload activity with translations
function reloadActivity() {
    const activityList = document.getElementById('activity-list');
    activityList.innerHTML = '';

    loadInitialData();
}

// Initialize language on load
document.addEventListener('DOMContentLoaded', () => {
    updateUILanguage();
});
