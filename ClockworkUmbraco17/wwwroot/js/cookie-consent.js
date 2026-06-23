(function () {
    var STORAGE_KEY = "kelimebull-cookie-consent";
    var banner = document.getElementById("cookie-consent");
    var acceptBtn = document.querySelector("[data-cookie-consent-accept]");

    if (!banner || !acceptBtn) {
        return;
    }

    if (localStorage.getItem(STORAGE_KEY) === "accepted") {
        return;
    }

    banner.removeAttribute("hidden");
    banner.setAttribute("aria-hidden", "false");

    acceptBtn.addEventListener("click", function () {
        localStorage.setItem(STORAGE_KEY, "accepted");
        banner.setAttribute("hidden", "");
        banner.setAttribute("aria-hidden", "true");
    });
})();
