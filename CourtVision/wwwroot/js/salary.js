// Salary Cap Game: зареждане на снимките предварително и скриване на краткото съобщение
"use strict"; // Строг режим

// Настройва таблото (при зареждане и след всяка подмяна)
function initSalary(root) {
    // Съобщението "Play today's game to unlock" изчезва след 2.8 секунди
    root.querySelectorAll("[data-autohide]").forEach(el => setTimeout(() => el.classList.add("gone"), 2800));
    // Зареждаме всички снимки от мрежата веднага (за да няма празни карти след разбъркване)
    root.querySelectorAll(".sc-card img").forEach(img => { const pre = new Image(); pre.src = img.src; });
}

// След всяка подмяна на таблото
CV.after.push(root => initSalary(root));

// Първоначална настройка
initSalary(document);
