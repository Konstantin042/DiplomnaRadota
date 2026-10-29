// Guess the Player: курсорът отива в полето за име при зареждане и след всеки опит
"use strict"; // Строг режим

// Слага фокуса в полето за име (ако го има)
function focusGuess(root) {
    // Полето за име
    const input = root.querySelector("#gpName");
    // Ако играта тече, курсорът отива там
    if (input) input.focus();
}

// След всяка подмяна на таблото
CV.after.push(root => focusGuess(root));

// Първоначална настройка
focusGuess(document);
