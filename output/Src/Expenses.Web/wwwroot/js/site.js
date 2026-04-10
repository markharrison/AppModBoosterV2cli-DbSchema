// Theme toggle
(function () {
    const themeToggle = document.getElementById('themeToggle');
    const html = document.documentElement;

    function getStoredTheme() {
        return localStorage.getItem('theme');
    }

    function applyTheme(theme) {
        html.setAttribute('data-theme', theme);
        const icon = themeToggle?.querySelector('i');
        if (icon) {
            icon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
        }
    }

    // Apply stored theme or system preference
    const stored = getStoredTheme();
    if (stored) {
        applyTheme(stored);
    } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        applyTheme('dark');
    }

    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            const current = html.getAttribute('data-theme') || 'light';
            const next = current === 'dark' ? 'light' : 'dark';
            localStorage.setItem('theme', next);
            applyTheme(next);
        });
    }
})();

// Confirmation dialogs
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.confirm-action');
    if (btn) {
        const message = btn.getAttribute('data-confirm') || 'Are you sure?';
        if (!confirm(message)) {
            e.preventDefault();
            e.stopPropagation();
        }
    }
});
