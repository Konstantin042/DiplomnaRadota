// Общ JavaScript за целия сайт: прозорецът "How to play" и формулярите без презареждане
"use strict"; // Строг режим: хваща често срещани грешки

// Общ обект, в който страниците регистрират своите функции
window.CV = {
    // Функции, които се изпълняват след подмяна на съдържанието
    after: [],
    // Функции, които се изпълняват преди изпращане; връщат колко милисекунди да трае анимацията
    before: [],
    // Истина, ако потребителят е поискал по-малко анимации
    reduce: window.matchMedia("(prefers-reduced-motion: reduce)").matches,
};

// Изчакване за дадени милисекунди (връща Promise)
CV.sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

// Отваря прозореца "How to play" с текста от <template id="help-...">
CV.openHelp = name => {
    // Намираме шаблона с текста
    const tpl = document.getElementById("help-" + name);
    // Ако няма такъв, не правим нищо
    if (!tpl) return;
    // Слагаме текста в прозореца
    const dlg = document.getElementById("dlg");
    dlg.innerHTML = "";
    dlg.appendChild(tpl.content.cloneNode(true));
    // Показваме прозореца
    document.getElementById("modal").hidden = false;
    // Слагаме фокуса на бутона, за да работи и с клавиатура
    const btn = dlg.querySelector("[data-close]");
    if (btn) btn.focus();
};

// Затваря прозореца "How to play"
CV.closeHelp = () => { document.getElementById("modal").hidden = true; };

// Кликове навсякъде по страницата
document.addEventListener("click", e => {
    // Бутон "?" отваря правилата на играта
    const help = e.target.closest("[data-help]");
    if (help) CV.openHelp(help.dataset.help);
    // Бутон "Let's play" или клик извън прозореца го затваря
    if (e.target.closest("#modal [data-close]") || e.target.id === "modal") CV.closeHelp();
});

// Клавиш Escape затваря прозореца
document.addEventListener("keydown", e => { if (e.key === "Escape") CV.closeHelp(); });

// При първо посещение на игра показваме правилата автоматично (веднъж на сесия)
(() => {
    // Намираме бутона "?" на страницата
    const help = document.querySelector("[data-help]");
    // Ако няма, това не е страница с игра
    if (!help) return;
    // Ключът за тази игра в sessionStorage
    const key = "cv-help-" + help.dataset.help;
    try {
        // Ако вече сме ги показали, спираме
        if (sessionStorage.getItem(key)) return;
        // Отбелязваме, че ги показваме
        sessionStorage.setItem(key, "1");
    } catch (err) { /* В някои браузъри sessionStorage е забранен: тогава просто ги показваме */ }
    // Отваряме правилата
    CV.openHelp(help.dataset.help);
})();

// Формуляри с data-ajax: изпращат се с fetch и подменят само част от страницата
document.addEventListener("submit", async e => {
    // Формулярът, който се изпраща
    const form = e.target;
    // Само формуляри с data-ajax="#контейнер"
    const target = form.dataset.ajax && document.querySelector(form.dataset.ajax);
    // Иначе браузърът го изпраща нормално (с презареждане)
    if (!target) return;
    // Спираме нормалното изпращане
    e.preventDefault();
    // Бутонът, който е натиснат (носи formaction, name и value)
    const submitter = e.submitter;
    // Адресът: от бутона или от формуляра
    const url = (submitter && submitter.getAttribute("formaction")) || form.action;
    // Данните на формуляра заедно с name/value на бутона
    const data = new FormData(form, submitter);
    // Колко да трае анимацията преди подмяната (страниците могат да я удължат)
    let wait = 0;
    for (const fn of CV.before) wait = Math.max(wait, fn(form, submitter) || 0);
    // Заключваме бутоните, за да не се натиска два пъти
    form.querySelectorAll("button").forEach(b => b.disabled = true);
    try {
        // Изпращаме заявката и чакаме и анимацията
        const [res] = await Promise.all([
            fetch(url, { method: "POST", body: data, headers: { "X-Requested-With": "fetch" } }),
            CV.sleep(CV.reduce ? 0 : wait),
        ]);
        // Ако сървърът върне грешка, презареждаме страницата
        if (!res.ok) { location.reload(); return; }
        // Слагаме новия HTML в контейнера
        target.innerHTML = await res.text();
        // Изпълняваме функциите след подмяната (с бутона, за да знаят какво е натиснато)
        CV.after.forEach(fn => fn(target, submitter));
    } catch (err) {
        // При мрежова грешка презареждаме страницата
        location.reload();
    }
});
