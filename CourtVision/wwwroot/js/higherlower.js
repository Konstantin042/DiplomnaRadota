// Higher or Lower: числото на десния играч брои от 0 нагоре, а след верен отговор следва нов рунд сам
"use strict"; // Строг режим

// Настройва картата (при зареждане и след всяка подмяна)
function initHigherLower(root) {
    // Числото за броене (има го само след отговор)
    const num = root.querySelector("[data-count]");
    // Бутонът за следващ рунд (има го само след верен отговор)
    const next = root.querySelector("[data-auto-next]");
    // Крайната стойност
    const target = num ? parseFloat(num.dataset.count) : 0;
    // Броене от 0 до стойността за 700 ms
    if (num && !CV.reduce) {
        // Началното време
        const t0 = performance.now();
        // Една стъпка от анимацията
        const step = now => {
            // Каква част от времето е минала (0 до 1)
            const k = Math.min(1, (now - t0) / 700);
            // Показваме междинната стойност с един знак след точката
            num.textContent = (target * k).toFixed(1);
            // Продължаваме до края
            if (k < 1) requestAnimationFrame(step);
        };
        // Стартираме
        requestAnimationFrame(step);
    }
    // След верен отговор: следващ рунд след 1.6 секунди
    if (next) setTimeout(() => { if (document.body.contains(next)) next.form.requestSubmit(next); }, CV.reduce ? 900 : 1600);
}

// След всяка подмяна на картата
CV.after.push(root => initHigherLower(root));

// Първоначална настройка
initHigherLower(document);
