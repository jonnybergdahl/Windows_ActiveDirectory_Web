function toggleDropdown() {
    document.getElementById("profileDropdown").classList.toggle("show");
}

window.onclick = function(event) {
    if (!event.target.matches('.profile-icon')) {
        var dropdowns = document.getElementsByClassName("dropdown-content");
        for (var i = 0; i < dropdowns.length; i++) {
            var openDropdown = dropdowns[i];
            if (openDropdown.classList.contains('show')) {
                openDropdown.classList.remove('show');
            }
        }
    }
}

async function logout() {
    await fetch('/api/auth/logout', { 
        method: 'POST',
        headers: { 'X-XSRF-TOKEN': sessionStorage.getItem('csrf') || '' }
    });
    sessionStorage.removeItem('csrf');
    sessionStorage.removeItem('username');
    window.location.href = '/login.html';
}

function updateHeaderUser() {
    const user = sessionStorage.getItem('username');
    if (user) {
        document.getElementById('header-username').textContent = user;
    }
}

function showStatus(msg, isError) {
    const container = document.getElementById('status-container');
    const toast = document.createElement('div');
    toast.className = `toast ${isError ? 'error' : 'success'}`;
    
    const text = document.createElement('span');
    text.textContent = msg;
    
    const close = document.createElement('span');
    close.className = 'toast-close';
    close.innerHTML = '&times;';
    close.onclick = () => toast.remove();
    
    toast.appendChild(text);
    toast.appendChild(close);
    container.appendChild(toast);
    
    setTimeout(() => {
        if (toast.parentElement) {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.5s ease';
            setTimeout(() => toast.remove(), 500);
        }
    }, 5000);
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.style.display = 'none';
}

function openConfirmModal(title, message, onConfirm) {
    document.getElementById('confirmTitle').textContent = title;
    document.getElementById('confirmMessage').textContent = message;
    const confirmBtn = document.getElementById('confirmBtn');
    
    // Remove old listeners
    const newConfirmBtn = confirmBtn.cloneNode(true);
    confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);
    
    newConfirmBtn.onclick = () => {
        onConfirm();
        closeModal('confirmModal');
    };
    
    document.getElementById('confirmModal').style.display = 'block';
}

function escapeHtml(str) {
    if (!str) return '';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

document.addEventListener('DOMContentLoaded', updateHeaderUser);
