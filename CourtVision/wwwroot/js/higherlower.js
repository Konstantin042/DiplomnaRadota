// Higher or Lower: числото на десния играч брои от 0 нагоре, а след верен отговор следва нов рунд сам
"use strict";

// Настройва картата (при зареждане и след всяка подмяна)
function initHigherLower(root) {
    const num = root.querySelector("[data-count]");
    const next = root.querySelector("[data-auto-next]");
    const target = num ? parseFloat(num.dataset.count) : 0;
    if (num && !CV.reduce) {
        const t0 = performance.now();
        // Една стъпка от анимацията
        const step = now => {
            const k = Math.min(1, (now - t0) / 700);
            num.textContent = (target * k).toFixed(1);
            if (k < 1) requestAnimationFrame(step);
        };
        requestAnimationFrame(step);
    }
    if (next) setTimeout(() => { if (document.body.contains(next)) next.form.requestSubmit(next); }, CV.reduce ? 900 : 1600);
}

CV.after.push(root => initHigherLower(root));

initHigherLower(document);
