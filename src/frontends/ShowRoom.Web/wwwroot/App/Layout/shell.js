// Interactivité de la coquille (menu mobile + retour en haut), en JS pur.
//
// Ces deux comportements sont purement locaux : ils ne lisent ni n'écrivent aucune donnée serveur.
// Les confier à un composant Blazor interactif ouvrirait un circuit SignalR sur CHAQUE page, y compris
// les écrans rendus en SSR statique — ce qui annulerait le bénéfice du SSR. Ils vivent donc ici.
//
// Le script est ré-exécutable : la navigation enrichie (blazor.web.js) remplace le DOM sans recharger
// la page, donc on se rebranche à chaque navigation plutôt que de mémoriser des références.

const SCROLL_TOP_THRESHOLD = 300;

function header() {
    return {
        toggle: document.querySelector('.menu-toggle'),
        nav: document.querySelector('.site-nav'),
        backdrop: document.querySelector('.menu-backdrop'),
    };
}

function setMenu(open) {
    const { toggle, nav, backdrop } = header();
    if (!toggle || !nav) {
        return;
    }

    nav.classList.toggle('is-open', open);
    backdrop?.classList.toggle('is-open', open);
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    toggle.setAttribute('aria-label', open ? 'Fermer le menu' : 'Ouvrir le menu');
}

function updateScrollTop() {
    document.querySelector('.scroll-top')
        ?.classList.toggle('is-visible', window.scrollY > SCROLL_TOP_THRESHOLD);
}

// Un seul jeu d'écouteurs délégués sur le document : ils survivent au remplacement du DOM.
function register() {
    document.addEventListener('click', (event) => {
        const target = event.target;

        if (target.closest('.menu-toggle')) {
            const isOpen = header().nav?.classList.contains('is-open') ?? false;
            setMenu(!isOpen);
            return;
        }

        // Un clic sur le fond ou sur un lien du menu referme.
        if (target.closest('.menu-backdrop') || target.closest('.site-nav a')) {
            setMenu(false);
            return;
        }

        if (target.closest('.scroll-top')) {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    });

    window.addEventListener('scroll', updateScrollTop, { passive: true });
    window.addEventListener('resize', updateScrollTop, { passive: true });

    // La navigation enrichie remplace le contenu : on repart d'un menu fermé et d'un bouton à jour.
    document.addEventListener('enhancedload', () => {
        setMenu(false);
        updateScrollTop();
    });

    updateScrollTop();
}

register();
