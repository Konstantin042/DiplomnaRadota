// NBA 101: натискане на позиция върху игрището подсвечва описанието ѝ
"use strict"; // Строг режим

// Слушаме кликове върху игрището
document.getElementById("court101").addEventListener("click", e => {
    // Бутонът на позицията (ако е натиснат)
    const spot = e.target.closest("[data-spot]");
    // Ако не е натиснат бутон, спираме
    if (!spot) return;
    // Кодът на позицията, например "PG"
    const code = spot.dataset.spot;
    // Маркираме натиснатия бутон и махаме маркировката от другите
    document.querySelectorAll("#court101 [data-spot]").forEach(b => b.classList.toggle("target", b === spot));
    // Подсвечваме описанието на същата позиция
    document.querySelectorAll("#poslist [data-pos]").forEach(p => p.classList.toggle("hl", p.dataset.pos === code));
});
