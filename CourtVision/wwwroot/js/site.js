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

// Настройките от зъбното колело (тема, анимации, снимки, "How to play"), пазят се в localStorage
const SETTINGS_KEY = "cv-settings";
const defaultSettings = () => ({
    motion: !window.matchMedia("(prefers-reduced-motion: reduce)").matches,
    photos: true,
    help: true,
    light: false,
});
let SET = defaultSettings();
try {
    Object.assign(SET, JSON.parse(localStorage.getItem(SETTINGS_KEY) || "{}"));
} catch (err) { /* няма localStorage: остават стандартните */ }

// Слага класовете на <html> и оцветява бутоните в менюто
function applySettings() {
    const root = document.documentElement;
    root.classList.toggle("no-motion", !SET.motion);
    root.classList.toggle("no-photos", !SET.photos);
    root.classList.toggle("light", !!SET.light);
    document.querySelectorAll("#setMenu .sw").forEach(b => b.setAttribute("aria-checked", String(!!SET[b.dataset.set])));
    document.querySelectorAll("#setMenu [data-theme]").forEach(b => b.setAttribute("aria-pressed", String((b.dataset.theme === "1") === !!SET.light)));
    CV.reduce = !SET.motion;
}

function saveSettings() {
    try { localStorage.setItem(SETTINGS_KEY, JSON.stringify(SET)); } catch (err) { }
    applySettings();
}

// Смяна на темата: новите цветове минават през страницата като вълна отляво надясно
function changeTheme(light) {
    if (!!SET.light === light) return;
    const swap = () => { SET.light = light; saveSettings(); };
    if (!document.startViewTransition || !SET.motion) { swap(); return; }

    const w = window.innerWidth, h = window.innerHeight;
    const amp = Math.min(70, w * 0.08);
    // Многоъгълник с вълнист десен край; p е докъде е стигнала вълната (0..1)
    const wave = (p, phase) => {
        const x0 = -2 * amp + p * (w + 4 * amp);
        const points = ["0px 0px"];
        for (let i = 0; i <= 24; i++) {
            const x = x0 + amp * Math.sin(i / 24 * Math.PI * 3 + phase);
            points.push(`${x.toFixed(1)}px ${(h * i / 24).toFixed(1)}px`);
        }
        points.push(`0px ${h}px`);
        return `polygon(${points.join(",")})`;
    };
    const frames = [];
    for (let k = 0; k <= 14; k++) frames.push({ clipPath: wave(k / 14, k * 0.7) });

    const transition = document.startViewTransition(swap);
    transition.ready.then(() => document.documentElement.animate(frames, {
        duration: 1800,
        easing: "ease-in-out",
        pseudoElement: "::view-transition-new(root)",
    })).catch(() => { });
}

function openSettings(open) {
    document.getElementById("setMenu").hidden = !open;
    document.getElementById("setBtn").setAttribute("aria-expanded", String(open));
}

document.getElementById("setBtn").addEventListener("click", e => {
    e.stopPropagation();
    openSettings(document.getElementById("setMenu").hidden);
});

document.getElementById("setMenu").addEventListener("click", e => {
    const toggle = e.target.closest(".sw");
    if (toggle) { SET[toggle.dataset.set] = !SET[toggle.dataset.set]; saveSettings(); return; }
    const theme = e.target.closest("[data-theme]");
    if (theme) { changeTheme(theme.dataset.theme === "1"); return; }
    if (e.target.closest("#setReset")) {
        SET = defaultSettings();
        try { localStorage.removeItem(SETTINGS_KEY); } catch (err) { }
        applySettings();
    }
});

// Клик извън менюто или Escape го затваря
document.addEventListener("click", e => { if (!e.target.closest(".set-wrap")) openSettings(false); });
document.addEventListener("keydown", e => { if (e.key === "Escape") openSettings(false); });

applySettings();

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
    if (!help || !SET.help) return;
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
