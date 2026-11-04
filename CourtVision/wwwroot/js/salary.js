// Salary Cap Game: зареждане на снимките предварително и скриване на краткото съобщение
"use strict";

// Настройва таблото (при зареждане и след всяка подмяна)
function initSalary(root) {
    root.querySelectorAll("[data-autohide]").forEach(el => setTimeout(() => el.classList.add("gone"), 2800));
    root.querySelectorAll(".sc-card img").forEach(img => { const pre = new Image(); pre.src = img.src; });
}

CV.after.push(root => initSalary(root));

initSalary(document);
