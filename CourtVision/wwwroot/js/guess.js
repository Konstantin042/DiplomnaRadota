// Guess the Player: подсказки докато пишеш (всеки играч, чието име или фамилия започва с написаното) и фокус в полето
"use strict"; // Строг режим

// Всички имена (идват от сървъра като JSON в страницата)
const GP_NAMES = JSON.parse(document.getElementById("gpNames").textContent);

// Намира играчите за написания текст: първо тези, чието име започва с него, после тези с такава фамилия
function gpMatches(text) {
    // Написаното с малки букви
    const q = text.trim().toLowerCase();
    // Празно поле: няма подсказки
    if (!q) return [];
    // Името започва с текста
    const first = GP_NAMES.filter(n => n.toLowerCase().startsWith(q));
    // Някоя друга дума (фамилията) започва с текста
    const other = GP_NAMES.filter(n => !n.toLowerCase().startsWith(q) && n.toLowerCase().split(/[\s-]+/).slice(1).some(w => w.startsWith(q)));
    // Връщаме всички
    return first.concat(other);
}

// Настройва полето за име и списъка с подсказки
function initGuess(root) {
    // Полето, списъкът и бутонът Guess
    const input = root.querySelector("#gpName");
    const box = root.querySelector("#gpSuggest");
    const guess = root.querySelector("[data-guess]");
    // Ако играта е свършила, няма поле
    if (!input || !box) return;
    // Курсорът отива в полето
    input.focus();
    // Кой ред е маркиран със стрелките (-1 = никой)
    let active = -1;
    // Показва подсказките за текущия текст
    const show = () => {
        // Подходящите имена
        const list = gpMatches(input.value);
        // Ако няма, скриваме списъка
        active = -1;
        if (!list.length) { box.hidden = true; box.innerHTML = ""; return; }
        // Един бутон за всяко име (текстът се слага безопасно чрез textContent)
        box.innerHTML = "";
        list.forEach(name => {
            const b = document.createElement("button");
            b.type = "button";
            b.className = "gp-opt";
            b.setAttribute("role", "option");
            b.textContent = name;
            box.appendChild(b);
        });
        // Показваме списъка
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
        // Всички редове
        const opts = [...box.querySelectorAll(".gp-opt")];
        if (!opts.length) return;
        // Новият маркиран ред (кръгово)
        active = (i + opts.length) % opts.length;
        opts.forEach((o, k) => o.classList.toggle("on", k === active));
        // Превъртаме до него
        opts[active].scrollIntoView({ block: "nearest" });
    };
    // При писане показваме подсказките
    input.addEventListener("input", show);
    // Клавиши: стрелки, Enter и Escape
    input.addEventListener("keydown", e => {
        if (e.key === "ArrowDown") { e.preventDefault(); if (box.hidden) show(); mark(active + 1); }
        else if (e.key === "ArrowUp") { e.preventDefault(); mark(active - 1); }
        else if (e.key === "Escape") { box.hidden = true; }
        else if (e.key === "Enter" && !box.hidden && active >= 0) { e.preventDefault(); pick(box.querySelectorAll(".gp-opt")[active].textContent); }
    });
    // Клик на име го избира (mousedown, за да не изгуби полето фокуса преди това)
    box.addEventListener("mousedown", e => {
        const o = e.target.closest(".gp-opt");
        if (o) { e.preventDefault(); pick(o.textContent); }
    });
    // Когато полето изгуби фокус, скриваме списъка
    input.addEventListener("blur", () => setTimeout(() => { box.hidden = true; }, 150));
}

// След всяка подмяна на таблото
CV.after.push(root => initGuess(root));

// Първоначална настройка
initGuess(document);
