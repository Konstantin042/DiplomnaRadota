// 82-0: анимация на барабаните, търсене/филтри/подреждане на играчите и анимация на сезона
"use strict"; // Строг режим

// Съкращенията на отборите за въртенето на барабана
const TEAMS82 = ["ATL","BOS","BKN","CHA","CHI","CLE","DAL","DEN","DET","GSW","HOU","IND","LAC","LAL","MEM","MIA","MIL","MIN","NOP","NYK","OKC","ORL","PHI","PHX","POR","SAC","SAS","TOR","UTA","WAS"];
// Десетилетията за въртенето
const ERAS82 = ["60's","70's","80's","90's","00's","10's","20's"];
// Случаен елемент от масив
const pick82 = list => list[Math.floor(Math.random() * list.length)];

// Преди изпращане: ако е завъртане, въртим барабаните
CV.before.push((form, btn) => {
    // Само за бутоните Spin / Respin
    if (!btn || !btn.dataset.spin) return 0;
    // Видът на завъртането: all, team или era
    const mode = btn.dataset.spin;
    // Барабаните, които се въртят
    const team = form.querySelector('[data-reel="team"]');
    const era = form.querySelector('[data-reel="era"]');
    // Добавяме клас за мигане
    form.querySelectorAll(".reel").forEach(r => r.classList.add("spin"));
    // На всеки 80 ms сменяме текста
    const timer = setInterval(() => {
        if (team && mode !== "era") team.textContent = pick82(TEAMS82); // Въртим отбора
        if (era && mode !== "team") era.textContent = pick82(ERAS82);   // Въртим ерата
    }, 80);
    // Спираме въртенето малко преди новото табло
    setTimeout(() => clearInterval(timer), 950);
    // Анимацията трае 1 секунда
    return 1000;
});

// След подмяна на таблото: настройваме списъка и анимацията на сезона
CV.after.push((root, btn) => {
    initDrawer(root);                                       // Търсене и филтри
    initSeason(root, btn && btn.hasAttribute("data-sim"));  // Анимация само след "Simulate"
});

// Настройва търсенето, филтрите и подреждането в чекмеджето с играчи
function initDrawer(root) {
    // Списъкът с играчи
    const list = root.querySelector("#plist");
    // Ако няма отворено чекмедже, спираме
    if (!list) return;
    // Полето за търсене, менюто за подреждане и филтрите
    const search = root.querySelector("#psearch");
    const sort = root.querySelector("#psort");
    const chips = root.querySelectorAll("[data-f]");
    // Текущият филтър (ALL, G, F или C)
    let filter = "ALL";
    // Прилага търсенето, филтъра и подреждането
    const apply = () => {
        // Търсеният текст с малки букви
        const q = search.value.trim().toLowerCase();
        // Всички редове
        const rows = [...list.querySelectorAll(".prow")];
        // Скриваме редовете, които не отговарят
        rows.forEach(r => {
            // Отговаря ли на името
            const byName = !q || r.dataset.name.includes(q);
            // Отговаря ли на филтъра (G = PG/SG, F = SF/PF, C = C)
            const byPos = filter === "ALL" || r.dataset.pos.split(" ").some(p => p.endsWith(filter));
            // Показваме или скриваме
            r.hidden = !(byName && byPos);
        });
        // Подреждаме: първо тези с място, после по избрания показател
        const key = sort.value;
        rows.sort((a, b) => (b.dataset.fit - a.dataset.fit) || (parseFloat(b.dataset[key]) - parseFloat(a.dataset[key])))
            .forEach(r => list.appendChild(r));
    };
    // Търсене при писане
    search.addEventListener("input", apply);
    // Enter в търсачката не изпраща формуляра
    search.addEventListener("keydown", e => { if (e.key === "Enter") e.preventDefault(); });
    // Подреждане при смяна
    sort.addEventListener("change", apply);
    // Филтрите
    chips.forEach(c => c.addEventListener("click", () => {
        filter = c.dataset.f;                                  // Запомняме филтъра
        chips.forEach(x => x.classList.toggle("on", x === c)); // Маркираме натиснатия
        apply();                                               // Прилагаме
    }));
    // Натискане на играч показва бутоните за място (и скрива другите)
    list.addEventListener("click", e => {
        // Редът на играча
        const toggle = e.target.closest("[data-toggle]");
        if (!toggle) return;
        // Целият ред и неговите бутони
        const row = toggle.closest(".prow");
        const place = row.querySelector(".place");
        // Дали вече е отворен
        const open = !place.hidden;
        // Затваряме всички
        list.querySelectorAll(".prow").forEach(r => { r.classList.remove("open"); r.querySelector(".place").hidden = true; });
        // Отваряме натиснатия, ако е бил затворен
        if (!open) { row.classList.add("open"); place.hidden = false; }
    });
}

// Анимация на сезона: мачовете се показват един по един
function initSeason(root, animate) {
    // Панелът на сезона
    const panel = root.querySelector("#season82");
    if (!panel) return;
    // Резултатите: низ от W и L
    const games = panel.dataset.games;
    // Точките за мачовете, броячите и крайният текст
    const dots = [...panel.querySelectorAll(".season i")];
    const w = panel.querySelector("[data-w]");
    const l = panel.querySelector("[data-l]");
    const verdict = panel.querySelector("[data-verdict]");
    // Показва мачовете до номер n
    const show = n => {
        // Броим победите и загубите до n
        let wins = 0, losses = 0;
        for (let i = 0; i < n; i++) {
            if (games[i] === "W") wins++; else losses++;           // Брояч
            dots[i].className = games[i] === "W" ? "w" : "l";      // Цвят на точката
        }
        w.textContent = wins;   // Победи
        l.textContent = losses; // Загуби
    };
    // Без анимация (или ако е изключена) показваме всичко веднага
    if (!animate || CV.reduce) { show(82); verdict.hidden = false; return; }
    // Превъртаме до панела
    panel.scrollIntoView({ behavior: "smooth", block: "center" });
    // Показваме по един мач на всеки 28 ms
    let i = 0;
    const timer = setInterval(() => {
        i++;
        show(i);
        if (i >= 82) { clearInterval(timer); verdict.hidden = false; } // Накрая показваме резултата
    }, 28);
}

// Първоначална настройка при зареждане на страницата
initDrawer(document);
initSeason(document, false);
