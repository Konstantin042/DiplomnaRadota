// Guess the Player: подсказки докато пишеш (всеки играч, чието име или фамилия започва с написаното) и фокус в полето
"use strict";

const GP_NAMES = JSON.parse(document.getElementById("gpNames").textContent);

// Намира играчите за написания текст: първо тези, чието име започва с него, после тези с такава фамилия
function gpMatches(text) {
    const q = text.trim().toLowerCase();
    if (!q) return [];
    const first = GP_NAMES.filter(n => n.toLowerCase().startsWith(q));
    const other = GP_NAMES.filter(n => !n.toLowerCase().startsWith(q) && n.toLowerCase().split(/[\s-]+/).slice(1).some(w => w.startsWith(q)));
    return first.concat(other);
}

// Настройва полето за име и списъка с подсказки
function initGuess(root) {
    const input = root.querySelector("#gpName");
    const box = root.querySelector("#gpSuggest");
    const guess = root.querySelector("[data-guess]");
    if (!input || !box) return;
    input.focus();
    let active = -1;
    // Показва подсказките за текущия текст
    const show = () => {
        const list = gpMatches(input.value);
        active = -1;
        if (!list.length) { box.hidden = true; box.innerHTML = ""; return; }
        box.innerHTML = "";
        list.forEach(name => {
            const b = document.createElement("button");
            b.type = "button";
            b.className = "gp-opt";
            b.setAttribute("role", "option");
            b.textContent = name;
            box.appendChild(b);
        });
        box.hidden = false;
    };
    // Избира име: слага го в полето и изпраща опита
    const pick = name => {
        input.value = name;
        box.hidden = true;
        input.form.requestSubmit(guess);
    };
    // Маркира ред със стрелките
    const mark = i => {
        const opts = [...box.querySelectorAll(".gp-opt")];
        if (!opts.length) return;
        active = (i + opts.length) % opts.length;
        opts.forEach((o, k) => o.classList.toggle("on", k === active));
        opts[active].scrollIntoView({ block: "nearest" });
    };
    input.addEventListener("input", show);
    input.addEventListener("keydown", e => {
        if (e.key === "ArrowDown") { e.preventDefault(); if (box.hidden) show(); mark(active + 1); }
        else if (e.key === "ArrowUp") { e.preventDefault(); mark(active - 1); }
        else if (e.key === "Escape") { box.hidden = true; }
        else if (e.key === "Enter" && !box.hidden && active >= 0) { e.preventDefault(); pick(box.querySelectorAll(".gp-opt")[active].textContent); }
    });
    box.addEventListener("mousedown", e => {
        const o = e.target.closest(".gp-opt");
        if (o) { e.preventDefault(); pick(o.textContent); }
    });
    input.addEventListener("blur", () => setTimeout(() => { box.hidden = true; }, 150));
}

CV.after.push(root => initGuess(root));

initGuess(document);
