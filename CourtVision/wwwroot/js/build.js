// Build-a-Player: анимация на барабаните и прозорецът за избор на отбор
"use strict";

const TEAMSB = ["ATL","BOS","BKN","CHA","CHI","CLE","DAL","DEN","DET","GSW","HOU","IND","LAC","LAL","MEM","MIA","MIL","MIN","NOP","NYK","OKC","ORL","PHI","PHX","POR","SAC","SAS","TOR","UTA","WAS"];
// Случаен елемент от масив
const pickB = list => list[Math.floor(Math.random() * list.length)];

CV.before.push((form, btn) => {
    if (!btn || !btn.hasAttribute("data-spin")) return 0;
    const teams = [...form.querySelectorAll("[data-reel-team] b")];
    const players = [...form.querySelectorAll("[data-reel-player]")];
    const names = players.map(p => p.textContent.trim()).filter(t => t && t !== "–");
    const timer = setInterval(() => {
        if (btn.value !== "player") teams.forEach(t => t.textContent = pickB(TEAMSB));
        players.forEach(p => { p.querySelector("b").textContent = names.length ? pickB(names).split(" ")[0] : "…"; });
    }, 75);
    setTimeout(() => clearInterval(timer), 1050);
    return 1100;
});

CV.after.push(root => initPicker(root));

// Прозорецът "Simulate season": табове Spin/Pick, търсене и завъртане на отбор
function initPicker(root) {
    const open = root.querySelector("[data-open-picker]");
    const modal = document.getElementById("tpModal");
    if (!open || !modal) return;
    open.addEventListener("click", () => { modal.hidden = false; });
    modal.addEventListener("click", e => { if (e.target === modal || e.target.closest("[data-tp-close]")) modal.hidden = true; });
    modal.querySelectorAll("[data-tp]").forEach(tab => tab.addEventListener("click", () => {
        modal.querySelectorAll("[data-tp]").forEach(t => t.classList.toggle("on", t === tab));
        modal.querySelectorAll("[data-tp-panel]").forEach(p => p.hidden = p.dataset.tpPanel !== tab.dataset.tp);
        modal.querySelector("[data-tp-title]").textContent = tab.dataset.tp === "pick" ? "Pick your team" : "Spin your team";
    }));
    const search = modal.querySelector("#tpSearch");
    search.addEventListener("input", () => {
        const q = search.value.trim().toLowerCase();
        modal.querySelectorAll(".tpm-team").forEach(b => b.hidden = q && !b.dataset.search.includes(q));
    });
    const reel = modal.querySelector("#tpReel");
    const spin = modal.querySelector("#tpSpin");
    const abbrs = reel.dataset.teams.split(",");
    const names = reel.dataset.names.split(",");
    const rows = [...reel.querySelectorAll(".tpm-row")];
    // Рисува барабана около отбор с номер i
    const draw = i => rows.forEach((row, k) => {
        const j = (i + k - 2 + abbrs.length * 3) % abbrs.length;
        row.querySelector("b").textContent = abbrs[j];
        row.querySelector("small").textContent = names[j];
    });
    spin.addEventListener("click", async () => {
        spin.disabled = true;
        // Заявка към SeasonController.SpinTeam
        const res = await fetch("/Season/SpinTeam", { method: "POST", headers: { "X-Requested-With": "fetch" } });
        const team = await res.json();
        const target = abbrs.indexOf(team.abbr);
        let i = 0;
        const steps = CV.reduce ? 0 : 24 + ((target - 24) % abbrs.length + abbrs.length) % abbrs.length;
        for (let n = 0; n < steps; n++) { draw(i++ % abbrs.length); await CV.sleep(40 + n * 4); }
        draw(target);
        const result = modal.querySelector("[data-tp-result]");
        result.querySelector("input[name=team]").value = team.abbr;
        result.querySelector("[data-tp-text]").textContent = `You'll play for the ${team.name} (${team.conference}).`;
        result.hidden = false;
        spin.hidden = true;
    });
}

initPicker(document);
