// ===========================================================
// app.js — Word Game frontend logic
//
// This file talks to the backend API and updates the page.
// No frameworks here — just plain JavaScript so it's easy to
// follow exactly what's happening at each step.
// ===========================================================

// --- Grab references to all the HTML elements we'll need ---
// Doing this once at the top means we don't have to repeat
// document.getElementById(...) everywhere below.

const setupScreen = document.getElementById("setup-screen");
const gameScreen = document.getElementById("game-screen");
const resultScreen = document.getElementById("result-screen");

const wordLengthInput = document.getElementById("word-length");
const startButton = document.getElementById("start-button");
const setupError = document.getElementById("setup-error");

const gameWordLengthLabel = document.getElementById("game-word-length");
const attemptsUsedLabel = document.getElementById("attempts-used");
const maxAttemptsLabel = document.getElementById("max-attempts");
const guessHistory = document.getElementById("guess-history");
const guessForm = document.getElementById("guess-form");
const guessInput = document.getElementById("guess-input");
const gameMessage = document.getElementById("game-message");

const resultHeading = document.getElementById("result-heading");
const resultWord = document.getElementById("result-word");
const continueButton = document.getElementById("continue-button");
const changeDifficultyButton = document.getElementById("change-difficulty-button");
const quitButton = document.getElementById("quit-button");

const recordDisplay = document.getElementById("record");

// --- App state ---
// These variables track everything happening "right now."
// Since we're not persisting anything yet, this all lives in
// memory and resets if the page is refreshed.

let currentGameId = null;   // the game we're currently playing
let currentWordLength = 5;  // remembered so "Continue" can reuse it
let wins = 0;
let losses = 0;

// --- Small helper functions for showing/hiding screens ---
// Only one "screen" should be visible at a time.

function showScreen(screenToShow) {
  setupScreen.classList.add("hidden");
  gameScreen.classList.add("hidden");
  resultScreen.classList.add("hidden");

  screenToShow.classList.remove("hidden");
}

function updateRecordDisplay() {
  recordDisplay.textContent = `Wins: ${wins} \u00A0 Losses: ${losses}`;
}

// ===========================================================
// Starting a new game
// ===========================================================

async function startGame(wordLength) {
  setupError.textContent = ""; // clear any old error message

  try {
    const response = await fetch("/api/games", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ wordLength: wordLength }),
    });

    if (!response.ok) {
      const errorBody = await response.json();
      setupError.textContent = errorBody.error || "Could not start game.";
      return;
    }

    const data = await response.json();
    // data looks like: { gameId, wordLength, maxAttempts }

    currentGameId = data.gameId;
    currentWordLength = data.wordLength;

    // Reset the game screen for this new game
    guessHistory.innerHTML = "";
    gameMessage.textContent = "";
    attemptsUsedLabel.textContent = "0";
    maxAttemptsLabel.textContent = data.maxAttempts;
    gameWordLengthLabel.textContent = data.wordLength;
    guessInput.value = "";
    guessInput.maxLength = data.wordLength;

    showScreen(gameScreen);
    guessInput.focus();
  } catch (err) {
    setupError.textContent = "Network error — is the server running?";
  }
}

startButton.addEventListener("click", () => {
  const wordLength = parseInt(wordLengthInput.value, 10);

  if (isNaN(wordLength) || wordLength < 3 || wordLength > 15) {
    setupError.textContent = "Please enter a word length between 3 and 15.";
    return;
  }

  startGame(wordLength);
});

// ===========================================================
// Submitting a guess
// ===========================================================

guessForm.addEventListener("submit", async (event) => {
  event.preventDefault(); // stop the form from reloading the page

  const guess = guessInput.value.trim();
  if (guess.length === 0) return;

  try {
    const response = await fetch(`/api/games/${currentGameId}/guess`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ guess: guess }),
    });

    const data = await response.json();

    if (!response.ok) {
      // Backend rejected the guess (wrong length, not a real word, etc.)
      gameMessage.textContent = data.error || "Invalid guess.";
      return;
    }

    gameMessage.textContent = "";
    guessInput.value = "";

    // data looks like: { results, status, attemptsUsed, maxAttempts, targetWord }
    addGuessRow(guess, data.results);
    attemptsUsedLabel.textContent = data.attemptsUsed;

    if (data.status === "Won" || data.status === 1) {
      handleGameOver(true, data.targetWord);
    } else if (data.status === "Lost" || data.status === 2) {
      handleGameOver(false, data.targetWord);
    }
    // otherwise status is still "InProgress" — just keep playing
  } catch (err) {
    gameMessage.textContent = "Network error — please try again.";
  }
});

// Builds one row of colored letter tiles for a submitted guess.
function addGuessRow(guess, results) {
  const row = document.createElement("div");
  row.className = "guess-row";

  for (let i = 0; i < guess.length; i++) {
    const tile = document.createElement("div");
    tile.className = "tile " + letterResultToCssClass(results[i]);
    tile.textContent = guess[i];
    row.appendChild(tile);
  }

  guessHistory.appendChild(row);
}

// The backend's LetterResult enum can come through as either a string
// ("Correct") or a number (2), depending on serialization settings —
// this handles both so the frontend doesn't break either way.
function letterResultToCssClass(result) {
  if (result === "Correct" || result === 2) return "tile-correct";
  if (result === "Present" || result === 1) return "tile-present";
  return "tile-absent";
}

// ===========================================================
// Game over: show result screen, update the session record
// ===========================================================

function handleGameOver(didWin, targetWord) {
  if (didWin) {
    wins++;
    resultHeading.textContent = "You won! \uD83C\uDF89";
  } else {
    losses++;
    resultHeading.textContent = "You lost.";
  }

  updateRecordDisplay();
  resultWord.textContent = `The word was: ${targetWord.toUpperCase()}`;

  showScreen(resultScreen);
}

// ===========================================================
// Result screen buttons
// ===========================================================

continueButton.addEventListener("click", () => {
  // Same difficulty, brand new game
  startGame(currentWordLength);
});

changeDifficultyButton.addEventListener("click", () => {
  showScreen(setupScreen);
});

quitButton.addEventListener("click", () => {
  // Nothing to clean up server-side (games are in-memory and harmless
  // to just leave behind) — just send the player back to the start.
  currentGameId = null;
  showScreen(setupScreen);
});