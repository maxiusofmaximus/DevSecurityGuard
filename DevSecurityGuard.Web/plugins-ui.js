// Phase 10: Plugin UI JavaScript

async function loadPlugins() {
    const pluginList = document.getElementById('pluginList');
    const installedBody = document.getElementById('installedPluginsBody');

    try {
        // Fetch available plugins (mock data for now)
        const availablePlugins = [
            {
                id: 'community.ml-detector',
                name: 'ML Malware Detector',
                version: '1.0.0',
                author: 'Community',
                type: 'Detector',
                description: 'Advanced ML-based malware detection'
            },
            {
                id: 'enterprise.compliance',
                name: 'Enterprise Compliance',
                version: '2.1.0',
                author: 'Enterprise Team',
                type: 'Detector',
                description: 'License and policy compliance checks'
            }
        ];

        // Render available plugins
        pluginList.innerHTML = availablePlugins.map(plugin => `
            <div class="plugin-card">
                <h3>${plugin.name}</h3>
                <p>${plugin.description}</p>
                <div class="plugin-meta">
                    <span class="badge">${plugin.type}</span>
                    <span>v${plugin.version}</span>
                </div>
                <button onclick="installPlugin('${plugin.id}')">Install</button>
            </div>
        `).join('');

        // Fetch installed plugins from API
        const response = await fetch('http://localhost:5000/api/plugins');
        const installed = response.ok ? await response.json() : [];

        installedBody.innerHTML = installed.map(plugin => `
            <tr>
                <td>${plugin.name}</td>
                <td>${plugin.version}</td>
                <td><span class="badge">${plugin.type}</span></td>
                <td><span class="status-active">Active</span></td>
                <td>
                    <button onclick="uninstallPlugin('${plugin.id}')">Uninstall</button>
                </td>
            </tr>
        `).join('');

        if (installed.length === 0) {
            installedBody.innerHTML = '<tr><td colspan="5" style="text-align:center">No plugins installed</td></tr>';
        }
    } catch (error) {
        console.error('Error loading plugins:', error);
        pluginList.innerHTML = '<p>Error loading plugins</p>';
    }
}

async function installPlugin(pluginId) {
    try {
        const response = await fetch('http://localhost:5000/api/plugins/install', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ pluginId })
        });

        if (response.ok) {
            alert(`Plugin ${pluginId} installed successfully!`);
            await loadPlugins();
        } else {
            alert('Failed to install plugin');
        }
    } catch (error) {
        console.error('Error installing plugin:', error);
        alert('Error installing plugin');
    }
}

async function uninstallPlugin(pluginId) {
    if (!confirm(`Uninstall plugin ${pluginId}?`)) return;

    try {
        const response = await fetch(`http://localhost:5000/api/plugins/${pluginId}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            alert('Plugin uninstalled');
            await loadPlugins();
        } else {
            alert('Failed to uninstall plugin');
        }
    } catch (error) {
        console.error('Error uninstalling plugin:', error);
        alert('Error uninstalling plugin');
    }
}

async function refreshPlugins() {
    await loadPlugins();
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    loadPlugins();
});
