let allUsers = [];
let selectedGroupsForNewUser = [];
let userSort = { field: 'userLoginName', asc: true };

async function loadUserPaths() {
    const sel = document.getElementById('um-adpath');
    if (!sel) return;
    try {
        const res = await fetch('/api/users/paths');
        if (res.status === 401) {
            window.location.href = '/login.html';
            return;
        }
        if (!res.ok) throw new Error('Failed to load user paths');
        const paths = await res.json();
        if (!Array.isArray(paths) || paths.length === 0) {
            sel.innerHTML = '<option value="">No paths configured</option>';
            return;
        }
        sel.innerHTML = paths.map(p => `<option value="${escapeHtml(p)}">${escapeHtml(p)}</option>`).join('');
        loadUsers();
    } catch (e) {
        sel.innerHTML = '<option value="">Failed to load paths</option>';
    }
}

async function loadUsers() {
    const pathEl = document.getElementById('um-adpath');
    if (!pathEl) return;
    const path = pathEl.value;
    const tbody = document.getElementById('user-list');
    if (!path || !tbody) return;

    tbody.innerHTML = '<tr><td colspan="5" class="loading">Loading users...</td></tr>';
    try {
        const res = await fetch(`/api/users?adPath=${encodeURIComponent(path)}`);
        if (res.status === 401) {
            window.location.href = '/login.html';
            return;
        }
        if (!res.ok) throw new Error('Failed to load users');
        allUsers = await res.json();
        renderUsers();
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="5" class="loading">Failed to load users</td></tr>';
        showStatus('Failed to load users', true);
    }
}

function renderUsers() {
    const searchInput = document.getElementById('search');
    if (!searchInput) return;
    const search = searchInput.value.toLowerCase();
    let filtered = allUsers.filter(u => 
        (u.userLoginName || '').toLowerCase().includes(search) ||
        (u.firstName || '').toLowerCase().includes(search) ||
        (u.lastName || '').toLowerCase().includes(search)
    );

    filtered.sort((a, b) => {
        let valA = a[userSort.field] || '';
        let valB = b[userSort.field] || '';
        return userSort.asc ? valA.localeCompare(valB) : valB.localeCompare(valA);
    });

    const countEl = document.getElementById('userCount');
    if (countEl) {
        countEl.textContent = `Showing ${filtered.length} of ${allUsers.length} users`;
    }

    document.querySelectorAll('.sort-arrow').forEach(el => el.textContent = '');
    const arrow = document.getElementById(`sort-${userSort.field}`);
    if (arrow) arrow.textContent = userSort.asc ? '▲' : '▼';

    const tbody = document.getElementById('user-list');
    if (!tbody) return;
    if (filtered.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="loading">No users found</td></tr>';
        return;
    }

    tbody.innerHTML = filtered.map((u, i) => `
        <tr>
            <td>${i + 1}</td>
            <td>${escapeHtml(u.userLoginName)}</td>
            <td>${escapeHtml(u.firstName)}</td>
            <td>${escapeHtml(u.lastName)}</td>
            <td>
                <div class="action-buttons">
                    <button class="secondary" onclick="openPasswordModal('${escapeHtml(u.userLoginName)}')">Password</button>
                    <button class="primary" onclick="openGroupModal('${escapeHtml(u.userLoginName)}')">Groups</button>
                    <button class="danger" onclick="removeUserByLogin('${escapeHtml(u.userLoginName)}')">Delete</button>
                </div>
            </td>
        </tr>
    `).join('');
}

function sortUsers(field) {
    if (userSort.field === field) {
        userSort.asc = !userSort.asc;
    } else {
        userSort.field = field;
        userSort.asc = true;
    }
    renderUsers();
}

function filterUsers() {
    renderUsers();
}

async function createUser() {
    const login = document.getElementById('um-login').value.trim();
    const first = document.getElementById('um-first').value.trim();
    const last = document.getElementById('um-last').value.trim();
    const pass = document.getElementById('um-pass').value;

    if (!login || !first || !last || !pass) {
        showStatus('All fields are required', true);
        return;
    }

    try {
        const res = await fetch('/api/users', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
            },
            body: JSON.stringify({ 
                userLoginName: login, 
                firstName: first, 
                lastName: last, 
                password: pass, 
                adPath: document.getElementById('um-adpath').value,
                groups: selectedGroupsForNewUser
            })
        });
        let data;
        try {
            data = await res.json();
        } catch (jsonErr) {
            data = { message: 'Unexpected server response' };
        }
        if (res.ok) {
            showStatus(data.message, false);
            document.getElementById('um-login').value = '';
            document.getElementById('um-first').value = '';
            document.getElementById('um-last').value = '';
            document.getElementById('um-pass').value = '';
            selectedGroupsForNewUser = [];
            updateSelectedGroupsUI();
            loadUsers();
        } else {
            showStatus(data.message || `Error creating user (Status: ${res.status})`, true);
        }
    } catch (e) {
        console.error('Create user error:', e);
        showStatus('Network error', true);
    }
}

function addGroupToNewUser() {
    const select = document.getElementById('um-group-select');
    const group = select.value;
    if (group && !selectedGroupsForNewUser.includes(group)) {
        selectedGroupsForNewUser.push(group);
        updateSelectedGroupsUI();
    }
    select.value = '';
}

function removeGroupFromNewUser(group) {
    selectedGroupsForNewUser = selectedGroupsForNewUser.filter(g => g !== group);
    updateSelectedGroupsUI();
}

function updateSelectedGroupsUI() {
    const container = document.getElementById('selected-groups-list');
    if (!container) return;
    container.innerHTML = selectedGroupsForNewUser.map(g => `
        <span class="group-tag">${escapeHtml(g)} <span class="remove" onclick="removeGroupFromNewUser('${escapeHtml(g)}')">&times;</span></span>
    `).join('');
}

async function removeUserByLogin(login) {
    if (!login) return;
    openConfirmModal('Delete User', `Are you sure you want to delete user ${login}?`, async () => {
        try {
            const res = await fetch('/api/users', {
                method: 'DELETE',
                headers: { 
                    'Content-Type': 'application/json',
                    'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
                },
                body: JSON.stringify({ userLoginName: login })
            });
            let data;
            try {
                data = await res.json();
            } catch (jsonErr) {
                data = { message: 'Unexpected server response' };
            }
            if (res.ok) {
                showStatus(data.message, false);
                loadUsers();
            } else {
                showStatus(data.message || `Error removing user (Status: ${res.status})`, true);
            }
        } catch (e) {
            console.error('Remove user error:', e);
            showStatus('Network error', true);
        }
    });
}

function openPasswordModal(login) {
    const userEl = document.getElementById('pw-target-user');
    if (userEl) userEl.textContent = login;
    document.getElementById('pw-new-pass').value = '';
    document.getElementById('pw-confirm-pass').value = '';
    document.getElementById('passwordModal').style.display = 'block';
}

function openGroupModal(login) {
    const userEl = document.getElementById('gm-target-user');
    if (userEl) userEl.textContent = login;
    document.getElementById('groupModal').style.display = 'block';
    loadUserGroups(login);
}

async function loadUserGroups(login) {
    const tbody = document.getElementById('gm-current-groups-body');
    if (!tbody) return;
    tbody.innerHTML = '<tr><td>Loading...</td></tr>';
    try {
        const res = await fetch(`/api/users/groups/${encodeURIComponent(login)}`);
        if (res.status === 401) { window.location.href = '/login.html'; return; }
        if (!res.ok) throw new Error('Failed to load user groups');
        const groups = await res.json();
        renderUserGroups(login, groups);
    } catch (e) {
        tbody.innerHTML = '<tr><td class="error">Failed to load groups</td></tr>';
    }
}

function renderUserGroups(login, groups) {
    const tbody = document.getElementById('gm-current-groups-body');
    if (!tbody) return;
    if (!groups || groups.length === 0) {
        tbody.innerHTML = '<tr><td>Not a member of any groups.</td></tr>';
        return;
    }
    tbody.innerHTML = groups.map(g => `
        <tr>
            <td>${escapeHtml(g)}</td>
            <td style="text-align: right;">
                <button class="danger" style="padding: 4px 8px; font-size: 12px;" 
                        onclick="submitRemoveFromGroup('${escapeHtml(login)}', '${escapeHtml(g)}')">Delete</button>
            </td>
        </tr>
    `).join('');
}

async function submitChangePassword() {
    const loginEl = document.getElementById('pw-target-user');
    if (!loginEl) return;
    const login = loginEl.textContent;
    const pass = document.getElementById('pw-new-pass').value;
    const confirm = document.getElementById('pw-confirm-pass').value;

    if (!pass) {
        showStatus('New password required', true);
        return;
    }

    if (pass !== confirm) {
        showStatus('Passwords do not match', true);
        return;
    }

    try {
        const res = await fetch('/api/users/change-password', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
            },
            body: JSON.stringify({ userLoginName: login, newPassword: pass })
        });
        let data;
        try {
            data = await res.json();
        } catch (jsonErr) {
            data = { message: 'Unexpected server response' };
        }
        if (res.ok) {
            showStatus(data.message, false);
            closeModal('passwordModal');
        } else {
            showStatus(data.message || `Error changing password (Status: ${res.status})`, true);
        }
    } catch (e) {
        console.error('Change password error:', e);
        showStatus('Network error', true);
    }
}

async function submitAddToGroup() {
    const loginEl = document.getElementById('gm-target-user');
    if (!loginEl) return;
    const login = loginEl.textContent;
    const group = document.getElementById('gm-group-select').value;

    if (!group) {
        showStatus('Please select a group', true);
        return;
    }

    try {
        const res = await fetch('/api/users/add-to-group', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
            },
            body: JSON.stringify({ userLoginName: login, groupName: group })
        });
        let data;
        try {
            data = await res.json();
        } catch (jsonErr) {
            data = { message: 'Unexpected server response' };
        }
        if (res.ok) {
            showStatus(data.message, false);
            loadUserGroups(login);
        } else {
            showStatus(data.message || `Error adding to group (Status: ${res.status})`, true);
        }
    } catch (e) {
        console.error('Add to group error:', e);
        showStatus('Network error', true);
    }
}

async function submitRemoveFromGroup(login, group) {
    openConfirmModal('Delete from Group', `Are you sure you want to delete ${login} from ${group}?`, async () => {
        try {
            const res = await fetch('/api/users/remove-from-group', {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || ''
                },
                body: JSON.stringify({ userLoginName: login, groupName: group })
            });
            let data;
            try {
                data = await res.json();
            } catch (jsonErr) {
                data = { message: 'Unexpected server response' };
            }
            if (res.ok) {
                showStatus(data.message, false);
                loadUserGroups(login);
            } else {
                showStatus(data.message || `Error removing from group (Status: ${res.status})`, true);
            }
        } catch (e) {
            console.error('Remove from group error:', e);
            showStatus('Network error', true);
        }
    });
}

async function loadGroups() {
    try {
        const res = await fetch('/api/users/groups');
        if (res.status === 401) {
            window.location.href = '/login.html';
            return;
        }
        if (res.ok) {
            const groups = await res.json();
            const gmSelect = document.getElementById('gm-group-select');
            const umSelect = document.getElementById('um-group-select');
            if (!gmSelect || !umSelect) return;
            const options = (groups || []).map(g => `<option value="${escapeHtml(g.groupName)}">${escapeHtml(g.groupName)}</option>`).join('');
            gmSelect.innerHTML = '<option value="">Select group...</option>' + options;
            umSelect.innerHTML = '<option value="">Select group...</option>' + options;
        }
    } catch (e) {
        console.error('Failed to load groups', e);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    loadUserPaths();
    loadGroups();
});
