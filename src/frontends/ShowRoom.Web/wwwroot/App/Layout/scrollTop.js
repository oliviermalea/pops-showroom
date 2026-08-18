let dotNetRef;
let handler;
let timeoutId;

function updateVisibility() {
    if (!dotNetRef) {
        return;
    }

    const visible = window.scrollY > 300;
    dotNetRef.invokeMethodAsync('SetScrollTopVisibility', visible);
}

function scheduleUpdate() {
    window.clearTimeout(timeoutId);
    timeoutId = window.setTimeout(updateVisibility, 80);
}

export function register(callback) {
    dotNetRef = callback;
    handler = scheduleUpdate;

    window.addEventListener('scroll', handler, { passive: true });
    window.addEventListener('resize', handler, { passive: true });
    updateVisibility();
}

export function unregister() {
    window.removeEventListener('scroll', handler);
    window.removeEventListener('resize', handler);
    window.clearTimeout(timeoutId);
    dotNetRef = undefined;
    handler = undefined;
}
