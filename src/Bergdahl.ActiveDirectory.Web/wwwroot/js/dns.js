let allRecords = [];
let currentSort = { field: 'hostName', asc: true };

async function loadZones() {
    const sel = document.getElementById('zone');
    if (!sel) return;
    sel.innerHTML = '<option>Loading...</option>';
    try {
        const res = await fetch('/api/dns/zones');
        if (res.status === 401) {
            window.location.href = '/login.html';
            return;
        }
        if (!res.ok) throw new Error('Failed to load zones');
        const zones = await res.json();
        if (!Array.isArray(zones) || zones.length === 0) {
            sel.innerHTML = '<option value="">No zones configured</option>';
            return;
        }
        sel.innerHTML = zones.map(z => `<option value="${escapeHtml(z)}">${escapeHtml(z)}</option>`).join('');
        await loadRecords();
    } catch (e) {
        sel.innerHTML = '<option value="">Failed to load zones</option>';
    }
}

async function loadRecords() {
    const zoneEl = document.getElementById('zone');
    if (!zoneEl) return;
    const zone = zoneEl.value;
    const tbody = document.getElementById('records');
    if (!tbody) return;
    tbody.innerHTML = '<tr><td colspan="6" class="loading">Loading records...</td></tr>';

    try {
        const res = await fetch(`/api/dns/${zone}`);
        if (res.status === 401) {
            window.location.href = '/login.html';
            return;
        }
        if (!res.ok) throw new Error('Failed to load');

        allRecords = await res.json();
        updateTargetHostDropdown();
        renderRecords();
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="6" class="loading">Failed to load records</td></tr>';
        showStatus('Failed to load records', true);
    }
}

function updateTargetHostDropdown() {
    const sel = document.getElementById('targetHost');
    if (!sel) return;
    const currentValue = sel.value;
    const hostRecords = allRecords.filter(r => r.recordType === 'A');
    
    sel.innerHTML = '<option value="">Select Target Host...</option>' + 
        hostRecords.map(r => `<option value="${escapeHtml(r.hostName)}">${escapeHtml(r.hostName)}</option>`).join('');
    
    if (hostRecords.some(r => r.hostName === currentValue)) {
        sel.value = currentValue;
    }
}

function renderRecords() {
    const searchInput = document.getElementById('search');
    if (!searchInput) return;
    const search = searchInput.value.toLowerCase();
    let filtered = allRecords.filter(r =>
        r.hostName.toLowerCase().includes(search) ||
        (r.ipAddress || '').toLowerCase().includes(search)
    );

    // Sort
    filtered.sort((a, b) => {
        let valA = a[currentSort.field];
        let valB = b[currentSort.field];

        if (currentSort.field === 'ttl') {
            return currentSort.asc ? valA - valB : valB - valA;
        }
        if (currentSort.field === 'ipAddress') {
            // Sort IPs numerically
            const numA = (valA || '').split('.').reduce((acc, oct) => acc * 256 + (parseInt(oct) || 0), 0);
            const numB = (valB || '').split('.').reduce((acc, oct) => acc * 256 + (parseInt(oct) || 0), 0);
            return currentSort.asc ? numA - numB : numB - numA;
        }
        return currentSort.asc
            ? (valA || '').localeCompare(valB || '')
            : (valB || '').localeCompare(valA || '');
    });

    const countEl = document.getElementById('recordCount');
    if (countEl) {
        countEl.textContent = `Showing ${filtered.length} of ${allRecords.length} records`;
    }

    // Update sort arrows
    document.querySelectorAll('.sort-arrow').forEach(el => el.textContent = '');
    const arrow = document.getElementById(`sort-${currentSort.field}`);
    if (arrow) arrow.textContent = currentSort.asc ? '▲' : '▼';

    const tbody = document.getElementById('records');
    if (!tbody) return;
    if (filtered.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="loading">No records found</td></tr>';
        return;
    }

    tbody.innerHTML = filtered.map((r, i) => `
            <tr>
                <td>${i + 1}</td>
                <td>${escapeHtml(r.hostName)}</td>
                <td>${escapeHtml(r.recordType)}</td>
                <td>${escapeHtml(r.ipAddress)}</td>
                <td>${r.ttl}</td>
                <td><button class="danger" onclick="removeRecord('${escapeHtml(r.hostName)}')">Delete</button></td>
            </tr>
        `).join('');
}

function sortTable(field) {
    if (currentSort.field === field) {
        currentSort.asc = !currentSort.asc;
    } else {
        currentSort.field = field;
        currentSort.asc = true;
    }
    renderRecords();
}

function filterRecords() {
    renderRecords();
}

async function addRecord() {
    const zone = document.getElementById('zone').value;
    const hostname = document.getElementById('hostname').value.trim();
    const ip = document.getElementById('ip').value.trim();

    if (!hostname || !ip) {
        showStatus('Please enter both hostname and IP address', true);
        return;
    }

    // Basic validation
    if (!/^[a-zA-Z0-9\-\._]{1,255}$/.test(hostname)) {
        showStatus('Invalid hostname.', true);
        return;
    }
    if (!/^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(ip)) {
        showStatus('Invalid IP address format', true);
        return;
    }

    try {
        const res = await fetch('/api/dns', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
            },
            body: JSON.stringify({ zone, hostname, ipAddress: ip })
        });
        let data;
        try {
            data = await res.json();
        } catch (jsonErr) {
            // Handle non-JSON response
            const text = await res.text();
            data = { message: text || res.statusText };
        }

        if (res.ok) {
            showStatus(data.message, false);
            document.getElementById('hostname').value = '';
            document.getElementById('ip').value = '';
            loadRecords();
        } else {
            showStatus(data.message || 'Error adding record', true);
        }
    } catch (e) {
        showStatus('Network error', true);
    }
}

async function addCnameRecord() {
    const zone = document.getElementById('zone').value;
    const aliasName = document.getElementById('aliasName').value.trim();
    const targetHost = document.getElementById('targetHost').value;

    if (!aliasName || !targetHost) {
        showStatus('Please enter both alias name and select a target host', true);
        return;
    }

    if (!/^[a-zA-Z0-9\-\._]{1,255}$/.test(aliasName)) {
        showStatus('Invalid alias name.', true);
        return;
    }

    try {
        const res = await fetch('/api/dns/cname', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
            },
            body: JSON.stringify({ zone, aliasName, targetHost })
        });
        let data;
        try {
            data = await res.json();
        } catch (jsonErr) {
            const text = await res.text();
            data = { message: text || res.statusText };
        }

        if (res.ok) {
            showStatus(data.message, false);
            document.getElementById('aliasName').value = '';
            document.getElementById('targetHost').value = '';
            loadRecords();
        } else {
            showStatus(data.message || 'Error adding alias record', true);
        }
    } catch (e) {
        showStatus('Network error', true);
    }
}

async function removeRecord(hostname) {
    openConfirmModal('Delete Record', `Are you sure you want to delete ${hostname}?`, async () => {
        const zone = document.getElementById('zone').value;

        try {
            const res = await fetch('/api/dns', {
                method: 'DELETE',
                headers: { 
                    'Content-Type': 'application/json',
                    'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
                },
                body: JSON.stringify({ zone, hostname })
            });
            let data;
            try {
                data = await res.json();
            } catch (jsonErr) {
                const text = await res.text();
                data = { message: text || res.statusText };
            }

            if (res.ok) {
                showStatus(data.message, false);
                loadRecords();
            } else {
                showStatus(data.message || 'Error deleting record', true);
            }
        } catch (e) {
            showStatus('Network error', true);
        }
    });
}

// Initial load
document.addEventListener('DOMContentLoaded', loadZones);
