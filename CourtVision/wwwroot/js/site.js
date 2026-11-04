// Общ JavaScript за целия сайт: прозорецът "How to play" и формулярите без презареждане
"use strict";

// Общ обект, в който страниците регистрират своите функции
window.CV = {
    after: [],
    before: [],
    reduce: window.matchMedia("(prefers-reduced-motion: reduce)").matches,
};

// Изчакване за дадени милисекунди (връща Promise)
CV.sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

// Отваря прозореца "How to play" с текста от <template id="help-...">
CV.openHelp = name => {
    const tpl = document.getElementById("help-" + name);
    if (!tpl) return;
    const dlg = document.getElementById("dlg");
    dlg.innerHTML = "";
    dlg.appendChild(tpl.content.cloneNode(true));
    document.getElementById("modal").hidden = false;
    const btn = dlg.querySelector("[data-close]");
    if (btn) btn.focus();
};

// Затваря прозореца "How to play"
CV.closeHelp = () => { document.getElementById("modal").hidden = true; };

// Кликове навсякъде по страницата
document.addEventListener("click", e => {
    const help = e.target.closest("[data-help]");
    if (help) CV.openHelp(help.dataset.help);
    if (e.target.closest("#modal [data-close]") || e.target.id === "modal") CV.closeHelp();
});

// Клавиш Escape затваря прозореца
document.addEventListener("keydown", e => { if (e.key === "Escape") CV.closeHelp(); });

// При първо посещение на игра показваме правилата автоматично (веднъж на сесия)
(() => {
    const help = document.querySelector("[data-help]");
    if (!help) return;
    const key = "cv-help-" + help.dataset.help;
    try {
        if (sessionStorage.getItem(key)) return;
        sessionStorage.setItem(key, "1");
    } catch (err) { /* В някои браузъри sessionStorage е забранен: тогава просто ги показваме */ }
    CV.openHelp(help.dataset.help);
})();

// Формуляри с data-ajax: изпращат се с fetch и подменят само част от страницата
document.addEventListener("submit", async e => {
    const form = e.target;
    const target = form.dataset.ajax && document.querySelector(form.dataset.ajax);
    if (!target) return;
    if (e.submitter && e.submitter.hasAttribute("data-full")) return;
    e.preventDefault();
    const submitter = e.submitter;
    const url = (submitter && submitter.getAttribute("formaction")) || form.action;
    const data = new FormData(form, submitter);
    let wait = 0;
    for (const fn of CV.before) wait = Math.max(wait, fn(form, submitter) || 0);
    form.querySelectorAll("button").forEach(b => b.disabled = true);
    try {
        const [res] = await Promise.all([
            fetch(url, { method: "POST", body: data, headers: { "X-Requested-With": "fetch" } }),
            CV.sleep(CV.reduce ? 0 : wait),
        ]);
        if (!res.ok) { location.reload(); return; }
        target.innerHTML = await res.text();
        CV.after.forEach(fn => fn(target, submitter));
    } catch (err) {
        location.reload();
    }
});

// На тесен екран менюто горе се превърта: показваме връзката към текущата страница
(() => {
    const tabs = document.querySelector(".tabs");
    const current = tabs && tabs.querySelector('[aria-current="true"]');
    if (!current || tabs.scrollWidth <= tabs.clientWidth) return;
    tabs.scrollLeft = current.offsetLeft - (tabs.clientWidth - current.offsetWidth) / 2;
})();
