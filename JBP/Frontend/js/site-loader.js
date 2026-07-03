(() => {
    const dismissIntro = () => {
        const intro = document.getElementById('siteIntro');
        if (!intro) return;

        const path =
            window.location.pathname.split('/').pop().toLowerCase();

        const hash =
            window.location.hash.toLowerCase();

        const isHomeLoad =
            (path === '' || path === 'index.html') &&
            (hash === '' || hash === '#home');

        intro.classList.toggle('logo-only', !isHomeLoad);

        const duration =
            isHomeLoad ? 2250 : 1700;

        window.setTimeout(() => {
            intro.classList.add('is-leaving');
            window.setTimeout(() => intro.remove(), 550);
        }, duration);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', dismissIntro, { once: true });
    } else {
        dismissIntro();
    }
})();
