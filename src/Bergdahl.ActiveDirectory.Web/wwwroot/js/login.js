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

async function login() {
    const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            username: document.getElementById('username').value,
            password: document.getElementById('password').value
        })
    });
    if (res.ok) {
        const data = await res.json();
        sessionStorage.setItem('csrf', data.csrf);
        sessionStorage.setItem('username', data.user);
        window.location.href = '/';
    } else {
        const data = await res.json();
        showStatus(data.message || 'Login failed', true);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const passwordInput = document.getElementById('password');
    if (passwordInput) {
        passwordInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') login();
        });
    }
});
