// API Configuration
const API_BASE_URL = 'http://localhost:5000/api';
const SIGNALR_HUB_URL = 'http://localhost:5000/hubs/devsecurity';

// API Client
class DevSecurityAPI {
    constructor() {
        this.connection = null;
        this.isConnected = false;
    }

    // Initialize SignalR connection
    async connectSignalR() {
        try {
            // Note: In production, include @microsoft/signalr via CDN or npm
            if (typeof signalR === 'undefined') {
                console.warn('SignalR not loaded, real-time updates disabled');
                return;
            }

            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(SIGNALR_HUB_URL)
                .withAutomaticReconnect()
                .build();

            this.connection.on('ConfigUpdated', (config) => {
                console.log('Config updated:', config);
                this.handleConfigUpdate(config);
            });

            this.connection.on('ActivityUpdated', (activity) => {
                console.log('Activity updated:', activity);
                this.handleActivityUpdate(activity);
            });

            this.connection.on('StatsUpdated', (stats) => {
                console.log('Stats updated:', stats);
                this.handleStatsUpdate(stats);
            });

            await this.connection.start();
            this.isConnected = true;
            console.log('SignalR connected');
        } catch (error) {
            console.error('SignalR connection error:', error);
        }
    }

    // Get configuration
    async getConfig() {
        try {
            const response = await fetch(`${API_BASE_URL}/config`);
            return await response.json();
        } catch (error) {
            console.error('Error fetching config:', error);
            return this.getFallbackConfig();
        }
    }

    // Update single configuration value
    async updateConfig(key, value) {
        try {
            const response = await fetch(`${API_BASE_URL}/config/${key}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ value })
            });
            return await response.json();
        } catch (error) {
            console.error('Error updating config:', error);
            throw error;
        }
    }

    // Update multiple configuration values
    async updateConfigBatch(updates) {
        try {
            const response = await fetch(`${API_BASE_URL}/config/batch`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updates)
            });
            return await response.json();
        } catch (error) {
            console.error('Error updating config batch:', error);
            throw error;
        }
    }

    // Get activity/threats
    async getActivity(limit = 50) {
        try {
            const response = await fetch(`${API_BASE_URL}/activity?limit=${limit}`);
            return await response.json();
        } catch (error) {
            console.error('Error fetching activity:', error);
            return [];
        }
    }

    // Get statistics
    async getStats() {
        try {
            const response = await fetch(`${API_BASE_URL}/activity/stats`);
            return await response.json();
        } catch (error) {
            console.error('Error fetching stats:', error);
            return { threatsBlocked: 0, packagesScanned: 0, detectorsActive: 5 };
        }
    }

    // Fallback config for offline mode
    getFallbackConfig() {
        return {
            Language: localStorage.getItem('appLanguage') || 'en',
            InterventionMode: 'interactive',
            ForcePnpm: 'true',
            ProtectEnv: 'true',
            MonitorCredentials: 'true'
        };
    }

    // Handle config update from SignalR
    handleConfigUpdate(config) {
        if (config.Language && config.Language !== currentLanguage) {
            changeLanguage(config.Language);
        }

        // Update UI controls
        if (config.InterventionMode) {
            const select = document.getElementById('intervention-mode');
            if (select) select.value = config.InterventionMode;
        }

        if (typeof config.ForcePnpm !== 'undefined') {
            const checkbox = document.getElementById('force-pnpm');
            if (checkbox) checkbox.checked = config.ForcePnpm === 'true';
        }

        if (typeof config.ProtectEnv !== 'undefined') {
            const checkbox = document.getElementById('env-protection');
            if (checkbox) checkbox.checked = config.ProtectEnv === 'true';
        }

        if (typeof config.MonitorCredentials !== 'undefined') {
            const checkbox = document.getElementById('credential-monitoring');
            if (checkbox) checkbox.checked = config.MonitorCredentials === 'true';
        }

        addActivityItem('🔄 Settings synced from API', t('justNow'), 'info');
    }

    // Handle activity update from SignalR
    handleActivityUpdate(activity) {
        const message = `${activity.description || activity.packageName}`;
        const type = activity.wasBlocked ? 'danger' : 'warning';
        addActivityItem(message, t('justNow'), type);

        // Refresh stats
        this.loadStats();
    }

    // Handle stats update from SignalR
    handleStatsUpdate(stats) {
        if (stats.threatsBlocked !== undefined) {
            document.getElementById('threats-blocked').textContent = stats.threatsBlocked;
        }
        if (stats.packagesScanned !== undefined) {
            document.getElementById('packages-scanned').textContent = stats.packagesScanned;
        }
        if (stats.detectorsActive !== undefined) {
            document.getElementById('detectors-active').textContent = stats.detectorsActive;
        }
    }

    // Load and apply config
    async loadAndApplyConfig() {
        const config = await this.getConfig();

        // Apply language
        if (config.Language && config.Language !== currentLanguage) {
            changeLanguage(config.Language);
        }

        // Apply intervention mode
        if (config.InterventionMode) {
            const select = document.getElementById('intervention-mode');
            if (select) select.value = config.InterventionMode;
        }

        // Apply checkboxes
        const forcePnpm = document.getElementById('force-pnpm');
        if (forcePnpm) forcePnpm.checked = config.ForcePnpm === 'true';

        const protectEnv = document.getElementById('env-protection');
        if (protectEnv) protectEnv.checked = config.ProtectEnv === 'true';

        const credentials = document.getElementById('credential-monitoring');
        if (credentials) credentials.checked = config.MonitorCredentials === 'true';
    }

    // Load stats
    async loadStats() {
        const stats = await this.getStats();
        this.handleStatsUpdate(stats);
    }

    // Load activity
    async loadActivity() {
        const activities = await this.getActivity(20);

        // Clear existing
        const activityList = document.getElementById('activity-list');
        if (activityList) {
            activityList.innerHTML = '';
        }

        // Add activities
        activities.forEach(activity => {
            const message = activity.description || `📦 ${activity.packageName}`;
            const type = activity.wasBlocked ? 'danger' : activity.severity === 'High' ? 'warning' : 'info';
            const timeAgo = getTimeAgo(new Date(activity.detectedAt));
            addActivityItem(message, timeAgo, type);
        });
    }
}

// Helper: Get time ago string
function getTimeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);

    if (seconds < 60) return t('justNow');
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes} ${t('minutesAgo')}`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} hours ago`;
    return `${Math.floor(hours / 24)} days ago`;
}

// Global API instance
const api = new DevSecurityAPI();

// Initialize API on page load
document.addEventListener('DOMContentLoaded', async () => {
    await api.connectSignalR();
    await api.loadAndApplyConfig();
    await api.loadStats();
    await api.loadActivity();

    console.log('DevSecurityGuard API Client initialized');
});
