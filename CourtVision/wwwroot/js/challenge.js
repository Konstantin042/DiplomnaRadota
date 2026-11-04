// 82-0: анимация на барабаните, търсене/филтри/подреждане на играчите и анимация на сезона
"use strict";

const TEAMS82 = ["ATL","BOS","BKN","CHA","CHI","CLE","DAL","DEN","DET","GSW","HOU","IND","LAC","LAL","MEM","MIA","MIL","MIN","NOP","NYK","OKC","ORL","PHI","PHX","POR","SAC","SAS","TOR","UTA","WAS"];
const ERAS82 = ["60's","70's","80's","90's","00's","10's","20's"];
// Случаен елемент от масив
const pick82 = list => list[Math.floor(Math.random() * list.length)];

CV.before.push((form, btn) => {
    if (!btn || !btn.dataset.spin) return 0;
    const mode = btn.dataset.spin;
    const team = form.querySelector('[data-reel="team"]');
    const era = form.querySelector('[data-reel="era"]');
    form.querySelectorAll(".reel").forEach(r => r.classList.add("spin"));
    const timer = setInterval(() => {
        if (team && mode !== "era") team.textContent = pick82(TEAMS82);
        if (era && mode !== "team") era.textContent = pick82(ERAS82);
    }, 80);
    setTimeout(() => clearInterval(timer), 950);
    return 1000;
});

CV.after.push((root, btn) => {
    initDrawer(root);
    initSeason(root, btn && btn.hasAttribute("data-sim"));
});

// Настройва търсенето, филтрите и подреждането в чекмеджето с играчи
function initDrawer(root) {
    const list = root.querySelector("#plist");
    if (!list) return;
    const search = root.querySelector("#psearch");
    const sort = root.querySelector("#psort");
    const chips = root.querySelectorAll("[data-f]");
    let filter = "ALL";
    // Прилага търсенето, филтъра и подреждането
    const apply = () => {
        const q = search.value.trim().toLowerCase();
        const rows = [...list.querySelectorAll(".prow")];
        rows.forEach(r => {
            const byName = !q || r.dataset.name.includes(q);
            const byPos = filter === "ALL" || r.dataset.pos.split(" ").some(p => p.endsWith(filter));
            r.hidden = !(byName && byPos);
        });
        const key = sort.value;
        rows.sort((a, b) => (b.dataset.fit - a.dataset.fit) || (parseFloat(b.dataset[key]) - parseFloat(a.dataset[key])))
            .forEach(r => list.appendChild(r));
    };
    search.addEventListener("input", apply);
    search.addEventListener("keydown", e => { if (e.key === "Enter") e.preventDefault(); });
    sort.addEventListener("change", apply);
    chips.forEach(c => c.addEventListener("click", () => {
        filter = c.dataset.f;
        chips.forEach(x => x.classList.toggle("on", x === c));
        apply();
    }));
    list.addEventListener("click", e => {
        const toggle = e.target.closest("[data-toggle]");
        if (!toggle) return;
        const row = toggle.closest(".prow");
        const place = row.querySelector(".place");
        const open = !place.hidden;
        list.querySelectorAll(".prow").forEach(r => { r.classList.remove("open"); r.querySelector(".place").hidden = true; });
        if (!open) { row.classList.add("open"); place.hidden = false; }
    });
}

// Анимация на сезона: мачовете се показват един по един
function initSeason(root, animate) {
    const panel = root.querySelector("#season82");
    if (!panel) return;
    const games = panel.dataset.games;
    const dots = [...panel.querySelectorAll(".season i")];
    const w = panel.querySelector("[data-w]");
    const l = panel.querySelector("[data-l]");
    const verdict = panel.querySelector("[data-verdict]");
    // Показва мачовете до номер n
    const show = n => {
        let wins = 0, losses = 0;
        for (let i = 0; i < n; i++) {
            if (games[i] === "W") wins++; else losses++;
            dots[i].className = games[i] === "W" ? "w" : "l";
        }
        w.textContent = wins;
        l.textContent = losses;
    };
    if (!animate || CV.reduce) { show(82); verdict.hidden = false; return; }
    panel.scrollIntoView({ behavior: "smooth", block: "center" });
    let i = 0;
    const timer = setInterval(() => {
        i++;
        show(i);
        if (i >= 82) { clearInterval(timer); verdict.hidden = false; }
    }, 28);
}

initDrawer(document);
initSeason(document, false);
