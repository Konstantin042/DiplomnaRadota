// Сезонът: анимация на редовния сезон и плейофите на живо (резултатите са изчислени от C# на сървъра)
"use strict";

document.querySelectorAll("[data-view]").forEach(btn => btn.addEventListener("click", () => {
    document.querySelectorAll("[data-view]").forEach(b => b.classList.toggle("on", b === btn));
    document.querySelectorAll("[data-conf]").forEach(c => c.hidden = c.dataset.conf !== btn.dataset.view);
}));

// ---------- Стъпка 1: редовният сезон ----------
(() => {
    const rec = document.getElementById("spRec");
    if (!rec) return;
    const games = rec.dataset.games;
    const dots = [...document.querySelectorAll("#spDots i")];
    const w = rec.querySelector("[data-w]");
    const l = rec.querySelector("[data-l]");
    const after = document.getElementById("afterSeason");
    if (CV.reduce) return;
    dots.forEach(d => d.style.visibility = "hidden");
    after.hidden = true;
    let i = 0, wins = 0, losses = 0;
    const timer = setInterval(() => {
        if (games[i] === "W") wins++; else losses++;
        dots[i].style.visibility = "visible";
        w.textContent = wins;
        l.textContent = losses;
        i++;
        if (i >= 82) { clearInterval(timer); after.hidden = false; }
    }, 35);
})();

// ---------- Стъпка 2: плейофите на живо ----------
(() => {
    const dataEl = document.getElementById("spPath");
    if (!dataEl) return;
    const path = JSON.parse(dataEl.textContent);
    const start = document.getElementById("spStart");
    const seriesEl = document.getElementById("spSeries");
    const liveEl = document.getElementById("spLive");
    const ctrl = document.getElementById("spCtrl");
    const logEl = document.getElementById("spLog");
    const done = document.getElementById("spDone");
    let pi = 0;
    let gi = 0;
    let speed = 1.6;
    let paused = false;
    let finished = false;
    const us = document.querySelector(".sp-team .tb-badge").textContent.trim();
    const usColor = document.getElementById("season").dataset.color;

    // Обновява плейофната схема: кои кръгове се виждат и колко победи има всяка серия
    const drawBracket = () => {
        const cur = finished ? 4 : Math.max(0, path[pi] ? path[pi].round : 4);
        const played = path[pi] && path[pi].round >= 0 ? gi : 0;
        document.querySelectorAll(".sp-m[data-round]").forEach(box => {
            const r = +box.dataset.round;
            const tbd = box.nextElementSibling;
            const hide = r > cur;
            box.hidden = hide;
            if (tbd && tbd.hasAttribute("data-tbd-for")) tbd.hidden = !hide;
            if (hide) return;
            const g = box.dataset.games;
            const n = r < cur ? g.length : Math.min(played, g.length);
            const a = [...g.slice(0, n)].filter(x => x === "a").length;
            box.querySelector('[data-side="a"] [data-wins]').textContent = a;
            box.querySelector('[data-side="b"] [data-wins]').textContent = n - a;
            box.querySelectorAll("[data-side]").forEach(s => s.classList.toggle("win", n === g.length && ((s.dataset.side === "a") === (a > n - a))));
        });
    };

    // Рисува заглавието на серията и квадратчетата W/L
    const drawSeries = () => {
        const r = path[Math.min(pi, path.length - 1)];
        if (!r) { seriesEl.innerHTML = ""; return; }
        const g = r.games.slice(0, pi < path.length ? gi : r.games.length);
        const mine = g.filter(x => x.win).length;
        const boxes = Array.from({ length: r.single ? 1 : 7 }, (_, i) => g[i] ? `<i class="${g[i].win ? "w" : "l"}">${g[i].win ? "W" : "L"}</i>` : "<i></i>").join("");
        seriesEl.innerHTML = `<div class="sp-sr"><b>${r.name}</b><span>vs ${r.opp}</span></div><div class="sp-sq">${boxes}<em>${mine}–${g.length - mine}</em></div>`;
    };

    // Рисува таблото на мача на живо
    const drawLive = (L) => {
        const clock = Math.max(0, L.clock);
        const mm = Math.floor(clock / 60), ss = String(clock % 60).padStart(2, "0");
        const progress = ((L.q - 1) * 720 + (720 - clock)) / 2880 * 100;
        liveEl.innerHTML = `<div class="sp-live" style="--a:${usColor};--b:${L.oppColor}"><div class="sp-lh">${L.name} · Game ${L.k + 1}</div>
            <div class="sp-ls"><div><span class="tb-badge big" style="--tc:${usColor}">${us}</span><b class="${L.done && L.win ? "won" : ""}">${L.us}</b></div>
            <span class="sp-mid">${L.done ? '<i class="fin">FINAL</i>' : '<i class="lv">● LIVE</i>'}</span>
            <div><span class="tb-badge big" style="--tc:${L.oppColor}">${L.opp}</span><b class="${L.done && !L.win ? "won" : ""}">${L.them}</b></div></div>
            <div class="sp-lf"><span class="sp-q">${[1, 2, 3, 4].map(q => `<i class="${q <= L.q ? "on" : ""}"></i>`).join("")}</span><span>Q${L.q} · ${mm}:${ss}</span></div>
            <div class="sp-prog"><i style="width:${progress.toFixed(1)}%"></i></div></div>`;
    };

    // Добавя мач в дневника
    const addLog = (r, k, game) => {
        logEl.insertAdjacentHTML("beforeend", `<div class="${game.win ? "w" : "l"}"><span>${r.name} G${k + 1}</span><b>${game.win ? "W" : "L"} ${game.us}–${game.them}</b><span>vs ${r.opp}</span></div>`);
    };

    // Край на плейофите: показваме цялата схема и отиваме на отчета
    const finish = () => {
        finished = true;
        drawBracket();
        drawSeries();
        setTimeout(() => done.submit(), 1500);
    };

    // Играе един мач на живо (часовникът тече, резултатът расте до крайния)
    const playGame = async () => {
        const r = path[pi];
        const game = r.games[gi];
        // Състояние на мача
        const L = { name: r.name, k: gi, opp: r.opp, oppColor: r.oppColor, q: 1, clock: 720, us: 0, them: 0, win: game.win, done: false };
        while (true) {
            if (paused) { await CV.sleep(200); continue; }
            if (finished) return;
            L.clock -= 48;
            const frac = ((L.q - 1) * 720 + (720 - L.clock)) / 2880;
            L.us = Math.min(game.us, Math.round(game.us * frac + (Math.random() - .5) * 3));
            L.them = Math.min(game.them, Math.round(game.them * frac + (Math.random() - .5) * 3));
            if (L.clock <= 0) {
                if (L.q < 4) { L.q++; L.clock = 720; }
                else { L.us = game.us; L.them = game.them; L.done = true; drawLive(L); break; }
            }
            drawLive(L);
            await CV.sleep(CV.reduce ? 0 : 90 / speed);
        }
        addLog(r, gi, game);
        gi++;
        drawSeries();
        drawBracket();
        await CV.sleep(900 / speed);
    };

    // Главният цикъл: всички кръгове и мачове
    const run = async () => {
        while (pi < path.length && !finished) {
            const r = path[pi];
            while (gi < r.games.length && !finished) await playGame();
            if (finished) return;
            const wins = r.games.filter(g => g.win).length;
            const lost = wins < r.games.length - wins;
            if (lost) break;
            pi++; gi = 0;
            drawSeries();
            drawBracket();
        }
        if (!finished) finish();
    };

    document.getElementById("spTip").addEventListener("click", () => {
        start.hidden = true;
        ctrl.hidden = false;
        drawSeries();
        run();
    });

    ctrl.querySelectorAll("[data-spd]").forEach(b => b.addEventListener("click", () => {
        const v = +b.dataset.spd;
        if (v === 0) paused = !paused; else { paused = false; speed = v; }
        ctrl.querySelectorAll("[data-spd]").forEach(x => x.classList.toggle("on", (+x.dataset.spd === 0 && paused) || (!paused && +x.dataset.spd === speed)));
    }));

    document.getElementById("spSkip").addEventListener("click", () => { if (!finished) finish(); });

    document.getElementById("spLogB").addEventListener("click", e => {
        logEl.hidden = !logEl.hidden;
        e.target.textContent = logEl.hidden ? "View game log" : "Hide game log";
    });

    drawBracket();
})();
