// Сезонът: анимация на редовния сезон и плейофите на живо (резултатите са изчислени от C# на сървъра)
"use strict"; // Строг режим

// Превключвател West / East за плейофната схема
document.querySelectorAll("[data-view]").forEach(btn => btn.addEventListener("click", () => {
    // Маркираме натиснатия бутон
    document.querySelectorAll("[data-view]").forEach(b => b.classList.toggle("on", b === btn));
    // Показваме само избраната конференция
    document.querySelectorAll("[data-conf]").forEach(c => c.hidden = c.dataset.conf !== btn.dataset.view);
}));

// ---------- Стъпка 1: редовният сезон ----------
(() => {
    // Резултатът W–L и точките
    const rec = document.getElementById("spRec");
    // Ако не сме на стъпка 1, спираме
    if (!rec) return;
    // Резултатите: низ от W и L
    const games = rec.dataset.games;
    // Точките, броячите и частта след сезона
    const dots = [...document.querySelectorAll("#spDots i")];
    const w = rec.querySelector("[data-w]");
    const l = rec.querySelector("[data-l]");
    const after = document.getElementById("afterSeason");
    // Без анимация: всичко се вижда веднага
    if (CV.reduce) return;
    // Скриваме точките и частта след сезона
    dots.forEach(d => d.style.visibility = "hidden");
    after.hidden = true;
    // Броячи
    let i = 0, wins = 0, losses = 0;
    // На всеки 35 ms показваме следващия мач
    const timer = setInterval(() => {
        // Победа или загуба
        if (games[i] === "W") wins++; else losses++;
        // Показваме точката
        dots[i].style.visibility = "visible";
        // Обновяваме резултата
        w.textContent = wins;
        l.textContent = losses;
        i++;
        // След 82 мача показваме класирането и наградите
        if (i >= 82) { clearInterval(timer); after.hidden = false; }
    }, 35);
})();

// ---------- Стъпка 2: плейофите на живо ----------
(() => {
    // Данните за пътя ни (JSON от сървъра)
    const dataEl = document.getElementById("spPath");
    // Ако не сме на стъпка 2, спираме
    if (!dataEl) return;
    // Кръговете, които играем: име, номер, противник, мачове с резултати
    const path = JSON.parse(dataEl.textContent);
    // Елементите на страницата
    const start = document.getElementById("spStart");   // Бутонът Tip off
    const seriesEl = document.getElementById("spSeries"); // Серията
    const liveEl = document.getElementById("spLive");     // Мачът на живо
    const ctrl = document.getElementById("spCtrl");       // Бутоните за скорост
    const logEl = document.getElementById("spLog");       // Дневникът
    const done = document.getElementById("spDone");       // Формулярът за отчета
    // Състояние на анимацията
    let pi = 0;          // Кой кръг
    let gi = 0;          // Кой мач в кръга
    let speed = 1.6;     // Скорост (Fast по подразбиране)
    let paused = false;  // Пауза
    let finished = false; // Край
    // Нашият отбор (от картата на отбора)
    const us = document.querySelector(".sp-team .tb-badge").textContent.trim();
    // Цветът на нашия отбор
    const usColor = document.getElementById("season").dataset.color;

    // Обновява плейофната схема: кои кръгове се виждат и колко победи има всяка серия
    const drawBracket = () => {
        // Текущият кръг (Play-In = 0, след края = 4)
        const cur = finished ? 4 : Math.max(0, path[pi] ? path[pi].round : 4);
        // Колко мача от текущия кръг са изиграни
        const played = path[pi] && path[pi].round >= 0 ? gi : 0;
        // Обхождаме всички кутийки
        document.querySelectorAll(".sp-m[data-round]").forEach(box => {
            // Кръгът на серията
            const r = +box.dataset.round;
            // Празната кутийка "TBD" до нея
            const tbd = box.nextElementSibling;
            // Бъдещите кръгове са TBD
            const hide = r > cur;
            box.hidden = hide;
            if (tbd && tbd.hasAttribute("data-tbd-for")) tbd.hidden = !hide;
            if (hide) return;
            // Колко мача да покажем: всички за минали кръгове, иначе изиграните
            const g = box.dataset.games;
            const n = r < cur ? g.length : Math.min(played, g.length);
            // Победите на двата отбора до момента
            const a = [...g.slice(0, n)].filter(x => x === "a").length;
            box.querySelector('[data-side="a"] [data-wins]').textContent = a;
            box.querySelector('[data-side="b"] [data-wins]').textContent = n - a;
            // Победителят се маркира само ако серията е свършила
            box.querySelectorAll("[data-side]").forEach(s => s.classList.toggle("win", n === g.length && ((s.dataset.side === "a") === (a > n - a))));
        });
    };

    // Рисува заглавието на серията и квадратчетата W/L
    const drawSeries = () => {
        // Текущият кръг (или последният)
        const r = path[Math.min(pi, path.length - 1)];
        if (!r) { seriesEl.innerHTML = ""; return; }
        // Изиграните мачове
        const g = r.games.slice(0, pi < path.length ? gi : r.games.length);
        // Нашите победи и загуби в серията
        const mine = g.filter(x => x.win).length;
        // Квадратчетата (1 за Play-In, 7 за серия)
        const boxes = Array.from({ length: r.single ? 1 : 7 }, (_, i) => g[i] ? `<i class="${g[i].win ? "w" : "l"}">${g[i].win ? "W" : "L"}</i>` : "<i></i>").join("");
        seriesEl.innerHTML = `<div class="sp-sr"><b>${r.name}</b><span>vs ${r.opp}</span></div><div class="sp-sq">${boxes}<em>${mine}–${g.length - mine}</em></div>`;
    };

    // Рисува таблото на мача на живо
    const drawLive = (L) => {
        // Минутите и секундите на часовника
        const clock = Math.max(0, L.clock);
        const mm = Math.floor(clock / 60), ss = String(clock % 60).padStart(2, "0");
        // Колко от мача е изиграно (за лентата)
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
        finished = true;           // Отбелязваме края
        drawBracket();             // Пълна схема
        drawSeries();              // Последната серия
        setTimeout(() => done.submit(), 1500); // След 1.5 s изпращаме формуляра за отчета
    };

    // Играе един мач на живо (часовникът тече, резултатът расте до крайния)
    const playGame = async () => {
        // Текущият кръг и мач
        const r = path[pi];
        const game = r.games[gi];
        // Състояние на мача
        const L = { name: r.name, k: gi, opp: r.opp, oppColor: r.oppColor, q: 1, clock: 720, us: 0, them: 0, win: game.win, done: false };
        // Докато не свършат 4-те четвърти
        while (true) {
            // При пауза чакаме
            if (paused) { await CV.sleep(200); continue; }
            // Ако е натиснато Skip, спираме
            if (finished) return;
            // Часовникът тече с 48 секунди на стъпка
            L.clock -= 48;
            // Каква част от мача е изиграна
            const frac = ((L.q - 1) * 720 + (720 - L.clock)) / 2880;
            // Резултатът расте пропорционално (с малко случайност)
            L.us = Math.min(game.us, Math.round(game.us * frac + (Math.random() - .5) * 3));
            L.them = Math.min(game.them, Math.round(game.them * frac + (Math.random() - .5) * 3));
            // Край на четвъртата
            if (L.clock <= 0) {
                // Следваща четвърт или край на мача
                if (L.q < 4) { L.q++; L.clock = 720; }
                else { L.us = game.us; L.them = game.them; L.done = true; drawLive(L); break; }
            }
            // Рисуваме и чакаме според скоростта
            drawLive(L);
            await CV.sleep(CV.reduce ? 0 : 90 / speed);
        }
        // Записваме мача в дневника
        addLog(r, gi, game);
        // Следващ мач
        gi++;
        // Обновяваме серията и схемата
        drawSeries();
        drawBracket();
        // Кратка пауза след мача
        await CV.sleep(900 / speed);
    };

    // Главният цикъл: всички кръгове и мачове
    const run = async () => {
        // Докато има кръгове и не е натиснато Skip
        while (pi < path.length && !finished) {
            // Кръгът
            const r = path[pi];
            // Играем мачовете
            while (gi < r.games.length && !finished) await playGame();
            if (finished) return;
            // Загубили ли сме серията
            const wins = r.games.filter(g => g.win).length;
            const lost = wins < r.games.length - wins;
            // Ако сме загубили, плейофите свършват за нас
            if (lost) break;
            // Следващ кръг
            pi++; gi = 0;
            drawSeries();
            drawBracket();
        }
        // Край
        if (!finished) finish();
    };

    // Бутонът Tip off започва плейофите
    document.getElementById("spTip").addEventListener("click", () => {
        start.hidden = true;  // Скриваме бутона
        ctrl.hidden = false;  // Показваме контролите
        drawSeries();         // Първата серия
        run();                // Започваме
    });

    // Бутоните за скорост и пауза
    ctrl.querySelectorAll("[data-spd]").forEach(b => b.addEventListener("click", () => {
        // Стойността: 0 = пауза, иначе скорост
        const v = +b.dataset.spd;
        if (v === 0) paused = !paused; else { paused = false; speed = v; }
        // Маркираме активния бутон
        ctrl.querySelectorAll("[data-spd]").forEach(x => x.classList.toggle("on", (+x.dataset.spd === 0 && paused) || (!paused && +x.dataset.spd === speed)));
    }));

    // Skip: веднага отиваме на края
    document.getElementById("spSkip").addEventListener("click", () => { if (!finished) finish(); });

    // Показване/скриване на дневника
    document.getElementById("spLogB").addEventListener("click", e => {
        logEl.hidden = !logEl.hidden;
        e.target.textContent = logEl.hidden ? "View game log" : "Hide game log";
    });

    // Начално състояние на схемата: само първият кръг, 0 победи
    drawBracket();
})();
