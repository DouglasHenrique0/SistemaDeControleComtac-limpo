// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Interações globais devem permanecer imediatas. Movimentos específicos ficam no CSS.
const homeCards = document.querySelectorAll("[data-home-card]");

if (homeCards.length > 0) {
    if (!("IntersectionObserver" in window)) {
        homeCards.forEach((card) => card.classList.add("is-animation-visible"));
    } else {
        const cardObserver = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                entry.target.classList.toggle("is-animation-visible", entry.isIntersecting);
            });
        }, { threshold: 0.12 });

        homeCards.forEach((card) => cardObserver.observe(card));
        window.addEventListener("pagehide", () => cardObserver.disconnect(), { once: true });
    }
}

const closeNotification = (notification) => {
    if (!notification || notification.classList.contains("is-closing")) return;
    notification.classList.add("is-closing");
    window.setTimeout(() => notification.remove(), 180);
};

const prepareNotification = (notification) => {
    notification.querySelector("[data-notification-close]")
        ?.addEventListener("click", () => closeNotification(notification));

    const dismissAfter = Number.parseInt(notification.dataset.autoDismiss ?? "", 10);
    if (Number.isFinite(dismissAfter) && dismissAfter > 0) {
        window.setTimeout(() => closeNotification(notification), dismissAfter);
    }
};

document.querySelectorAll("[data-app-notification]").forEach(prepareNotification);

const firstServerError = document.querySelector("[data-focus-notification]");
if (firstServerError) {
    window.setTimeout(() => firstServerError.focus({ preventScroll: true }), 0);
}

const showClientValidationNotice = (form) => {
    if (document.querySelector("[data-client-validation-notice]")) return;

    const invalidFields = form.querySelectorAll(".input-validation-error, :invalid");
    if (invalidFields.length === 0) return;

    let stack = document.querySelector(".notification-stack");
    if (!stack) {
        stack = document.createElement("section");
        stack.className = "notification-stack";
        stack.setAttribute("aria-label", "Avisos do sistema");
        document.body.prepend(stack);
    }

    const notification = document.createElement("div");
    notification.className = "app-notification app-notification-error";
    notification.setAttribute("role", "alert");
    notification.setAttribute("tabindex", "-1");
    notification.dataset.appNotification = "";
    notification.dataset.clientValidationNotice = "";
    notification.innerHTML = `
        <span class="app-notification-icon" aria-hidden="true">!</span>
        <div class="app-notification-copy">
            <strong>Revise os campos</strong>
            <p>Há ${invalidFields.length} campo(s) que precisam de atenção antes de continuar.</p>
        </div>
        <button type="button" class="app-notification-close" data-notification-close aria-label="Fechar aviso">×</button>`;
    stack.append(notification);
    prepareNotification(notification);
    notification.focus({ preventScroll: true });
    invalidFields[0]?.focus();
};

document.querySelectorAll("form").forEach((form) => {
    form.addEventListener("submit", () => {
        window.setTimeout(() => showClientValidationNotice(form), 0);
    });
});
