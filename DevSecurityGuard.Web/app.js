// Initialize the application
document.addEventListener('DOMContentLoaded', () => {
    loadInitialData();
    updateStatistics();
});

// Load initial activity data
function loadInitialData() {
    addActivityItem('✅ Service started successfully', 'Just now', 'success');
    addActivityItem('📦 Analyzed package: react', '2 minutes ago', 'info');
    addActivityItem('🚫 Blocked typosquatting: reqest → request', '5 minutes ago', 'danger');
    addActivityItem('📦 Analyzed package: lodash', '10 minutes ago', 'info');
    addActivityItem('⚠️ Warning: Package published < 24h ago', '15 minutes ago', 'warning');
    addActivityItem('✅ All detectors loaded successfully', '30 minutes ago', 'success');
}

// Add activity item to the list
function addActivityItem(message, timeAgo, type = 'info') {
    const activityList = document.getElementById('activity-list');

    const item = document.createElement('div');
    item.className = 'activity-item';

    const messageSpan = document.createElement('div');
    messageSpan.className = `activity-message activity-${type}`;
    messageSpan.textContent = message;

    const timeSpan = document.createElement('div');
    timeSpan.className = 'activity-time';
    timeSpan.textContent = timeAgo;

    item.appendChild(messageSpan);
    item.appendChild(timeSpan);

    // Make scan reports clickable
    if (message.includes('Daily scan report') || message.includes('Package scan completed')) {
        item.style.cursor = 'pointer';
        item.onclick = () => viewScanReport(message);
        item.title = 'Click to view details';
    }

    // Insert at the beginning
    if (activityList.firstChild) {
        activityList.insertBefore(item, activityList.firstChild);
    } else {
        activityList.appendChild(item);
    }

    // Limit to 20 items
    while (activityList.children.length > 20) {
        activityList.removeChild(activityList.lastChild);
    }
}

// Update statistics
function updateStatistics() {
    document.getElementById('threats-blocked').textContent = '3';
    document.getElementById('packages-scanned').textContent = '47';
    document.getElementById('detectors-active').textContent = '5';
}

// Update intervention mode
function updateInterventionMode() {
    const select = document.getElementById('intervention-mode');
    const modeNames = {
        'automatic': 'Automatic',
        'interactive': 'Interactive',
        'alert': 'Alert Only'
    };

    const modeName = modeNames[select.value];
    const statusBadge = document.querySelector('.status-badge');
    const originalText = statusBadge.textContent;
    statusBadge.textContent = t('statusUpdating');

    // Save to API
    if (typeof api !== 'undefined') {
        api.updateConfig('InterventionMode', select.value).then(() => {
            statusBadge.textContent = originalText;
            addActivityItem(`⚙️ ${t('changedMode')}: ${modeName}`, t('justNow'), 'info');
        }).catch(error => {
            console.error('Failed to update:', error);
            statusBadge.textContent = originalText;
        });
    } else {
        setTimeout(() => {
            statusBadge.textContent = originalText;
            addActivityItem(`⚙️ Changed intervention mode to: ${modeName}`, 'Just now', 'info');
        }, 500);
    }
}

// Update settings
function updateSetting(setting, enabled) {
    const settingNames = {
        'pnpm': 'Force pnpm',
        'env': '.env protection',
        'credentials': 'Credential monitoring'
    };

    const settingKeys = {
        'pnpm': 'ForcePnpm',
        'env': 'ProtectEnv',
        'credentials': 'MonitorCredentials'
    };

    const status = enabled ? t('enabled') : t('disabled');

    // Save to API
    if (typeof api !== 'undefined') {
        api.updateConfig(settingKeys[setting], enabled.toString()).then(() => {
            addActivityItem(`⚙️ ${settingNames[setting]}: ${status}`, t('justNow'), 'info');
        }).catch(error => {
            console.error('Failed to update:', error);
        });
    } else {
        addActivityItem(`⚙️ ${settingNames[setting]}: ${status}`, 'Just now', 'info');
    }
}

// Restart service
function restartService() {
    if (confirm(t('restartConfirm'))) {
        const buttons = document.querySelectorAll('.btn-primary.btn-full');
        const button = buttons[0];
        const statusBadge = document.querySelector('.status-badge');

        button.textContent = t('restarting');
        button.disabled = true;
        statusBadge.textContent = t('statusRestarting');
        statusBadge.classList.remove('status-active');
        statusBadge.style.backgroundColor = '#FFA500';

        setTimeout(() => {
            button.textContent = t('restartService');
            button.disabled = false;
            statusBadge.textContent = t('statusActive');
            statusBadge.classList.add('status-active');
            statusBadge.style.backgroundColor = '';
            addActivityItem(t('serviceRestarted'), t('justNow'), 'success');
        }, 2000);
    }
}

// View logs
function viewLogs() {
    addActivityItem(t('openingLogs'), t('justNow'), 'info');
    alert(t('logsInfo'));
}

// Open settings
function openSettings() {
    showModal('Advanced Settings', `
        <div style="text-align: left;">
            <h3>🔐 Privacy Settings</h3>
            <label><input type="checkbox" id="modal-telemetry"> Enable telemetry (off by default)</label><br>
            <label><input type="checkbox" id="modal-threat-feed"> Enable community threat feed</label><br>
            <label><input type="checkbox" id="modal-encrypt-db"> Encrypt database</label><br><br>
            
            <h3>⚡ Performance Settings</h3>
            <label><input type="checkbox" id="modal-cache" checked> Enable caching</label><br>
            <label><input type="checkbox" id="modal-parallel" checked> Parallel scanning</label><br>
            <label>Max concurrency: <input type="number" id="modal-concurrency" value="4" min="1" max="16" style="width: 60px;"></label><br><br>
            
            <h3>📦 Package Managers</h3>
            <label><input type="checkbox" checked> npm</label><br>
            <label><input type="checkbox" checked> pip</label><br>
            <label><input type="checkbox" checked> cargo</label><br>
            <label><input type="checkbox"> nuget</label><br>
            <label><input type="checkbox"> maven</label><br>
        </div>
    `);
}

// View scan report
function viewScanReport(message) {
    const reportHtml = `
        <div style="text-align: left;">
            <h3>📊 Scan Report</h3>
            <p><strong>Scan Type:</strong> ${message.includes('Daily') ? 'Daily Automated Scan' : 'Package Scan'}</p>
            <p><strong>Status:</strong> <span style="color: #00C853;">✅ No threats detected</span></p>
            <p><strong>Packages Scanned:</strong> 47</p>
            <p><strong>Detectors Used:</strong> 5</p>
            <p><strong>Scan Duration:</strong> 1.2 seconds</p>
            <hr>
            <h4>Detectors Run:</h4>
            <ul>
                <li>✅ Typosquatting Detection</li>
                <li>✅ Credential Theft Detection</li>
                <li>✅ Malicious Script Detection</li>
                <li>✅ Supply Chain Analysis</li>
                <li>✅ Dependency Confusion Check</li>
            </ul>
            <hr>
            <h4>Recent Packages:</h4>
            <ul>
                <li>react (v18.2.0) - ✅ Clean</li>
                <li>lodash (v4.17.21) - ✅ Clean</li>
                <li>typescript (v5.0.0) - ✅ Clean</li>
            </ul>
        </div>
    `;
    showModal('Scan Report', reportHtml);
}

// Show modal
function showModal(title, content) {
    // Remove existing modal if any
    const existing = document.getElementById('custom-modal');
    if (existing) existing.remove();

    // Create modal
    const modal = document.createElement('div');
    modal.id = 'custom-modal';
    modal.innerHTML = `
        <div class="modal-overlay" onclick="closeModal()">
            <div class="modal-content" onclick="event.stopPropagation()">
                <div class="modal-header">
                    <h2>${title}</h2>
                    <button class="modal-close" onclick="closeModal()">×</button>
                </div>
                <div class="modal-body">
                    ${content}
                </div>
                <div class="modal-footer">
                    <button class="btn-primary" onclick="closeModal()">Close</button>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
}

// Close modal
function closeModal() {
    const modal = document.getElementById('custom-modal');
    if (modal) modal.remove();
}

// Simulation of real-time updates
let updateCounter = 0;
setInterval(() => {
    updateCounter++;

    if (updateCounter % 30 === 0) {
        const activities = [
            { msg: '📦 Analyzed package: typescript', type: 'info' },
            { msg: '✅ Package scan completed: no threats', type: 'success' },
            { msg: '📊 Daily scan report generated', type: 'info' }
        ];

        const activity = activities[Math.floor(Math.random() * activities.length)];
        addActivityItem(activity.msg, 'Just now', activity.type);

        const scanned = parseInt(document.getElementById('packages-scanned').textContent);
        document.getElementById('packages-scanned').textContent = scanned + 1;
    }
}, 1000);
