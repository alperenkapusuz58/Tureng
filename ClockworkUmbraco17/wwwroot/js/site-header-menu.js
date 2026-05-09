(function () {
    const menuRoot = document.getElementById("site-menu");
    const toggle = document.querySelector("[data-site-menu-open]");
    if (!menuRoot || !toggle) return;

    const closeSelector = "[data-site-menu-close]";

    function isMenuApplicable() {
        return window.matchMedia("(max-width: 1024px)").matches;
    }

    function openMenu() {
        if (!isMenuApplicable()) return;
        menuRoot.classList.add("is-open");
        menuRoot.removeAttribute("hidden");
        menuRoot.setAttribute("aria-hidden", "false");
        document.body.classList.add("site-menu--open");
        toggle.setAttribute("aria-expanded", "true");
        toggle.setAttribute("aria-label", "Menüyü kapat");
    }

    function closeMenu() {
        menuRoot.classList.remove("is-open");
        menuRoot.setAttribute("hidden", "");
        menuRoot.setAttribute("aria-hidden", "true");
        document.body.classList.remove("site-menu--open");
        toggle.setAttribute("aria-expanded", "false");
        toggle.setAttribute("aria-label", "Menüyü aç");
    }

    toggle.addEventListener("click", function () {
        if (!isMenuApplicable()) return;
        if (menuRoot.classList.contains("is-open")) {
            closeMenu();
        } else {
            openMenu();
        }
    });

    menuRoot.querySelectorAll(closeSelector).forEach(function (el) {
        el.addEventListener("click", function () {
            closeMenu();
        });
    });

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && menuRoot.classList.contains("is-open")) {
            closeMenu();
        }
    });

    window.addEventListener("resize", function () {
        if (!isMenuApplicable() && menuRoot.classList.contains("is-open")) {
            closeMenu();
        }
    });
})();
