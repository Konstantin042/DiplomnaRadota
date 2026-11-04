// NBA 101: натискане на позиция върху игрището подсвечва описанието ѝ
"use strict";

document.getElementById("court101").addEventListener("click", e => {
    const spot = e.target.closest("[data-spot]");
    if (!spot) return;
    const code = spot.dataset.spot;
    document.querySelectorAll("#court101 [data-spot]").forEach(b => b.classList.toggle("target", b === spot));
    document.querySelectorAll("#poslist [data-pos]").forEach(p => p.classList.toggle("hl", p.dataset.pos === code));
});
