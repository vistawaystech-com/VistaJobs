(() => {
    const dismissIntro = () => {
        const intro = document.getElementById('siteIntro');
        if (!intro) return;

        window.setTimeout(() => {
            intro.classList.add('is-leaving');
            window.setTimeout(() => intro.remove(), 550);
        }, 1750);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', dismissIntro, { once: true });
    } else {
        dismissIntro();
    }
})();
