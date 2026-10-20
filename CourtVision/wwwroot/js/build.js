// Build-a-Player: анимация на барабаните и прозорецът за избор на отбор
"use strict"; // Строг режим

// Отборите за въртенето на барабана
const TEAMSB = ["ATL","BOS","BKN","CHA","CHI","CLE","DAL","DEN","DET","GSW","HOU","IND","LAC","LAL","MEM","MIA","MIL","MIN","NOP","NYK","OKC","ORL","PHI","PHX","POR","SAC","SAS","TOR","UTA","WAS"];
// Случаен елемент от масив
const pickB = list => list[Math.floor(Math.random() * list.length)];

// Преди изпращане: при Spin въртим двата барабана
CV.before.push((form, btn) => {
    // Само за бутоните Spin / Respin
    if (!btn || !btn.hasAttribute("data-spin")) return 0;
    // Клетките на барабаните
    const teams = [...form.querySelectorAll("[data-reel-team] b")];
    const players = [...form.querySelectorAll("[data-reel-player]")];
    // Имената, които вече се виждат, за да ги разбъркваме
    const names = players.map(p => p.textContent.trim()).filter(t => t && t !== "–");
    // Сменяме текста на всеки 75 ms
    const timer = setInterval(() => {
        // Респин на играч не върти отбора
        if (btn.value !== "player") teams.forEach(t => t.textContent = pickB(TEAMSB));
        // Играчите се разместват
        players.forEach(p => { p.querySelector("b").textContent = names.length ? pickB(names).split(" ")[0] : "…"; });
    }, 75);
    // Спираме малко преди новия екран
    setTimeout(() => clearInterval(timer), 1050);
    // Анимацията трае 1.1 секунди
    return 1100;
});

// След подмяна на екрана настройваме прозореца за отбор
CV.after.push(root => initPicker(root));

// Прозорецът "Simulate season": табове Spin/Pick, търсене и завъртане на отбор
function initPicker(root) {
    // Бутонът, който отваря прозореца
    const open = root.querySelector("[data-open-picker]");
    // Самият прозорец
    const modal = document.getElementById("tpModal");
    // Ако ги няма (билдът не е готов), спираме
    if (!open || !modal) return;
    // Отваряне
    open.addEventListener("click", () => { modal.hidden = false; });
    // Затваряне с бутона X или с клик извън прозореца
    modal.addEventListener("click", e => { if (e.target === modal || e.target.closest("[data-tp-close]")) modal.hidden = true; });
    // Табовете Spin и Pick
    modal.querySelectorAll("[data-tp]").forEach(tab => tab.addEventListener("click", () => {
        // Маркираме натиснатия таб
        modal.querySelectorAll("[data-tp]").forEach(t => t.classList.toggle("on", t === tab));
        // Показваме съответния панел
        modal.querySelectorAll("[data-tp-panel]").forEach(p => p.hidden = p.dataset.tpPanel !== tab.dataset.tp);
        // Сменяме заглавието
        modal.querySelector("[data-tp-title]").textContent = tab.dataset.tp === "pick" ? "Pick your team" : "Spin your team";
    }));
    // Търсене в таба Pick
    const search = modal.querySelector("#tpSearch");
    search.addEventListener("input", () => {
        // Търсеният текст
        const q = search.value.trim().toLowerCase();
        // Скриваме отборите, които не съвпадат
        modal.querySelectorAll(".tpm-team").forEach(b => b.hidden = q && !b.dataset.search.includes(q));
    });
    // Барабанът за отбора и бутонът Spin
    const reel = modal.querySelector("#tpReel");
    const spin = modal.querySelector("#tpSpin");
    // Всички отбори и прякорите им (от data атрибутите)
    const abbrs = reel.dataset.teams.split(",");
    const names = reel.dataset.names.split(",");
    // Петте реда на барабана
    const rows = [...reel.querySelectorAll(".tpm-row")];
    // Рисува барабана около отбор с номер i
    const draw = i => rows.forEach((row, k) => {
        // Номерът на отбора в този ред (кръгово)
        const j = (i + k - 2 + abbrs.length * 3) % abbrs.length;
        row.querySelector("b").textContent = abbrs[j];       // Съкращение
        row.querySelector("small").textContent = names[j];   // Прякор
    });
    // Натискане на Spin: питаме сървъра (C#) за случаен отбор и анимираме до него
    spin.addEventListener("click", async () => {
        // Заключваме бутона
        spin.disabled = true;
        // Заявка към SeasonController.SpinTeam
        const res = await fetch("/Season/SpinTeam", { method: "POST", headers: { "X-Requested-With": "fetch" } });
        const team = await res.json();
        // Номерът на избрания отбор
        const target = abbrs.indexOf(team.abbr);
        // Въртим барабана: няколко обиколки и спираме на отбора
        let i = 0;
        const steps = CV.reduce ? 0 : 24 + ((target - 24) % abbrs.length + abbrs.length) % abbrs.length;
        for (let n = 0; n < steps; n++) { draw(i++ % abbrs.length); await CV.sleep(40 + n * 4); }
        draw(target);
        // Показваме резултата и бутона Start season
        const result = modal.querySelector("[data-tp-result]");
        result.querySelector("input[name=team]").value = team.abbr;
        result.querySelector("[data-tp-text]").textContent = `You'll play for the ${team.name} (${team.conference}).`;
        result.hidden = false;
        // Скриваме бутона Spin
        spin.hidden = true;
    });
}

// Първоначална настройка
initPicker(document);
