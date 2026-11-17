// Countdown of the current question. The server is the source of truth for the deadline:
// it sends the seconds left when the page is rendered, and this script only counts them down
// with the browser's monotonic clock (so a wrong PC clock does not matter) and reloads the page
// when they run out, so the server can apply the forced end.

(function () {
    const URGENT_THRESHOLD_SECONDS = 10;
    const RELOAD_DELAY_MILLISECONDS = 2500;
    const REFRESH_INTERVAL_MILLISECONDS = 250;

    /**
     * Computes the whole seconds left until the deadline.
     * @param {number} deadlineMonotonicMilliseconds - Deadline on the performance.now() timeline.
     * @returns {number} Remaining seconds, never negative.
     */
    function getRemainingSeconds(deadlineMonotonicMilliseconds) {
        return Math.max(0, Math.ceil((deadlineMonotonicMilliseconds - performance.now()) / 1000));
    }

    /**
     * Disables every answer and withdraw button so no further action is sent.
     * @param {HTMLElement} gameScreen - Container of the game screen.
     * @returns {void}
     */
    function disableGameActions(gameScreen) {
        gameScreen.querySelectorAll("button[type=submit]").forEach(function (actionButton) {
            actionButton.disabled = true;
        });
    }

    /**
     * Prevents double submissions: once a form is sent, all game buttons are disabled.
     * @param {HTMLElement} gameScreen - Container of the game screen.
     * @returns {void}
     */
    function preventDoubleSubmit(gameScreen) {
        gameScreen.querySelectorAll("form").forEach(function (gameForm) {
            gameForm.addEventListener("submit", function () {
                window.setTimeout(function () { disableGameActions(gameScreen); }, 0);
            });
        });
    }

    /**
     * Starts the countdown shown in the timer element.
     * @param {HTMLElement} timerElement - Element with the data-seconds-remaining attribute sent by the server.
     * @param {HTMLElement} gameScreen - Container of the game screen.
     * @returns {void}
     */
    function startCountdown(timerElement, gameScreen) {
        const secondsRemainingAtRender = Number(timerElement.dataset.secondsRemaining);
        const deadlineMonotonicMilliseconds = performance.now() + secondsRemainingAtRender * 1000;
        const intervalId = window.setInterval(function () {
            const remainingSeconds = getRemainingSeconds(deadlineMonotonicMilliseconds);
            timerElement.textContent = remainingSeconds + " s";
            timerElement.classList.toggle("round-timer--urgent", remainingSeconds <= URGENT_THRESHOLD_SECONDS);

            if (remainingSeconds === 0) {
                window.clearInterval(intervalId);
                disableGameActions(gameScreen);
                window.setTimeout(function () { window.location.reload(); }, RELOAD_DELAY_MILLISECONDS);
            }
        }, REFRESH_INTERVAL_MILLISECONDS);
    }

    const gameScreen = document.querySelector("[data-game-screen]");
    const timerElement = document.querySelector("[data-seconds-remaining]");
    if (gameScreen && timerElement) {
        preventDoubleSubmit(gameScreen);
        startCountdown(timerElement, gameScreen);
    }
})();
