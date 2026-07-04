// ===========================================================
// app.js — Word Game frontend logic
//
// This file talks to the backend API and updates the page.
// No frameworks here — just plain JavaScript so it's easy to
// follow exactly what's happening at each step.
// ===========================================================

// --- Grab references to all the HTML elements we'll need ---

const setupScreen = document.getElementById("setup-screen");
const gameScreen = document.getElementById("game-screen");
const resultScreen = document.getElementById("result-screen");

const wordLengthInput = document.getElementById("word-length");
const startButton = document.getElementById("start-button");
const setupError = document.getElementById("setup-error");

const gameWordLengthLabel = document.getElementById("game-word-length");
const attemptsUsedLabel = document.getElementById("attempts-used");
const maxAttemptsLabel = document.getElementById("max-attempts");
const guessBoard = document.getElementById("guess-board");
const gameMessage = document.getElementById("game-message");

const resultHeading = document.getElementById("result-heading");
const resultWord = document.getElementById("result-word");
const continueButton = document.getElementById("continue-button");
const changeDifficultyButton = document.getElementById("change-difficulty-button");
const quitButton = document.getElementById("quit-button");

const recordDisplay = document.getElementById("record");
const keyboard = document.getElementById("keyboard");

// --- App state ---

let currentGameId = null;
let currentWordLength = 5;
let wins = 0;
let losses = 0;
let letterResults = {};

// Tile grid state — tracks position and the DOM elements themselves
// tileGrid[row][col] gives us the exact tile div to update
let tileGrid = [];
let currentRow = 0;  // which attempt row we're on
let currentCol = 0;  // which tile in that row we're typing into
let isAcceptingInput = false; // false while waiting for API or game is over

// --- Helper functions ---

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
  setupError.textContent = "";

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
    // data: { gameId, wordLength, maxAttempts }

    currentGameId = data.gameId;
    currentWordLength = data.wordLength;
    currentRow = 0;
    currentCol = 0;
    isAcceptingInput = true;

    gameMessage.textContent = "";
    attemptsUsedLabel.textContent = "0";
    maxAttemptsLabel.textContent = data.maxAttempts;
    gameWordLengthLabel.textContent = data.wordLength;

    buildGuessBoard(data.wordLength, data.maxAttempts);

    letterResults = {};
    buildKeyboard();

    showScreen(gameScreen);
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
// Building the guess board
// ===========================================================

function buildGuessBoard(wordLength, maxAttempts) {
  guessBoard.innerHTML = "";
  tileGrid = [];

  // Only build the first row — new rows are added after each guess
  addNewRow(wordLength);
}

// Adds a single new empty row to the board for the current attempt
function addNewRow(wordLength) {
  const rowDiv = document.createElement("div");
  rowDiv.className = "guess-row";

  tileGrid[currentRow] = [];

  for (let col = 0; col < wordLength; col++) {
    const tile = document.createElement("div");
    tile.className = "tile";
    rowDiv.appendChild(tile);
    tileGrid[currentRow][col] = tile;
  }

  guessBoard.appendChild(rowDiv);
  highlightActiveTile();
}

// Marks the current tile as active (slightly darker border)
function highlightActiveTile() {
  // First clear active state from all tiles in the current row
  if (tileGrid[currentRow]) {
    for (const tile of tileGrid[currentRow]) {
      tile.classList.remove("tile-active");
    }
  }

  // Then mark just the current position as active
  if (tileGrid[currentRow] && tileGrid[currentRow][currentCol]) {
    tileGrid[currentRow][currentCol].classList.add("tile-active");
  }
}

// ===========================================================
// Keyboard input — typing goes directly into tiles
// ===========================================================

// Listen for keypresses anywhere on the page while the game screen is active.
// This means the player never needs to click a specific input box — just type.
document.addEventListener("keydown", (event) => {
  if (!isAcceptingInput) return; // ignore input while API call is in flight or game over

  const key = event.key;

  if (key === "Enter") {
    submitGuess();
  } else if (key === "Backspace") {
    deleteLetter();
  } else if (key.length === 1 && key.match(/[a-zA-Z]/)) {
    // Only accept single letter keys (ignores Tab, Shift, F1, etc.)
    typeLetter(key.toLowerCase());
  }
});

// Places a letter into the current tile and advances the cursor
function typeLetter(letter) {
  if (currentCol >= currentWordLength) return; // row is full, ignore

  const tile = tileGrid[currentRow][currentCol];
  tile.textContent = letter.toUpperCase();
  tile.classList.add("tile-filled");
  tile.classList.remove("tile-active");

  currentCol++;
  highlightActiveTile();
}

// Removes the last typed letter and moves the cursor back
function deleteLetter() {
  if (currentCol <= 0) return; // nothing to delete

  currentCol--;

  const tile = tileGrid[currentRow][currentCol];
  tile.textContent = "";
  tile.classList.remove("tile-filled");

  highlightActiveTile();
}

// ===========================================================
// Submitting a guess
// ===========================================================

async function submitGuess() {
  // Build the guess string from the current row's tile contents
  const guess = tileGrid[currentRow]
    .map(tile => tile.textContent.toLowerCase())
    .join("");

  if (guess.length < currentWordLength) {
    gameMessage.textContent = `Word must be ${currentWordLength} letters.`;
    return;
  } else {
      currentRow++;
      currentCol = 0;
      addNewRow(currentWordLength);
      isAcceptingInput = true;
      console.log("isAcceptingInput:", isAcceptingInput); // temporary debug
    }

  isAcceptingInput = false; // block input while waiting for the API
  gameMessage.textContent = "";

  try {
    const response = await fetch(`/api/games/${currentGameId}/guess`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ guess: guess }),
    });

    const data = await response.json();

    if (!response.ok) {
      gameMessage.textContent = data.error || "Invalid guess.";
      isAcceptingInput = true; // re-enable input so they can try again
      return;
    }

    // Color the tiles in the current row based on results
    colorCurrentRow(data.results);
    updateKeyboard(guess, data.results);
    attemptsUsedLabel.textContent = data.attemptsUsed;

    if (data.status === "Won") {
      handleGameOver(true, data.targetWord);
    } else if (data.status === "Lost") {
      handleGameOver(false, data.targetWord);
    } else {
      // Move to the next row and re-enable input
      currentRow++;
      currentCol = 0;
      highlightActiveTile();
      isAcceptingInput = true;
    }
  } catch (err) {
    gameMessage.textContent = "Network error — please try again.";
    isAcceptingInput = true;
  }
}

// Colors the tiles in the current row based on the API's results array
function colorCurrentRow(results) {
  for (let col = 0; col < results.length; col++) {
    const tile = tileGrid[currentRow][col];
    tile.classList.remove("tile-active", "tile-filled");
    tile.classList.add(letterResultToCssClass(results[col]));
  }
}

function letterResultToCssClass(result) {
  if (result === "Correct") return "tile-correct";
  if (result === "Present") return "tile-present";
  return "tile-absent";
}

// ===========================================================
// Keyboard tracker
// ===========================================================

function buildKeyboard() {
  const rows = [
    ["q", "w", "e", "r", "t", "y", "u", "i", "o", "p"],
    ["a", "s", "d", "f", "g", "h", "j", "k", "l"],
    ["z", "x", "c", "v", "b", "n", "m"],
  ];

  keyboard.innerHTML = "";

  for (const row of rows) {
    const rowDiv = document.createElement("div");
    rowDiv.className = "keyboard-row";

    for (const letter of row) {
      const key = document.createElement("div");
      key.className = "key";
      key.textContent = letter;
      key.id = `key-${letter}`;
      rowDiv.appendChild(key);
    }

    keyboard.appendChild(rowDiv);
  }
}

function updateKeyboard(guess, results) {
  const priority = { "Absent": 1, "Present": 2, "Correct": 3 };

  for (let i = 0; i < guess.length; i++) {
    const letter = guess[i];
    const result = results[i];

    const currentBest = letterResults[letter];
    if (!currentBest || priority[result] > priority[currentBest]) {
      letterResults[letter] = result;
    }

    const key = document.getElementById(`key-${letter}`);
    if (key) {
      key.classList.remove("tile-correct", "tile-present", "tile-absent");
      key.classList.add(letterResultToCssClass(letterResults[letter]));
    }
  }
}

// ===========================================================
// Game over
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
  isAcceptingInput = false;

  showScreen(resultScreen);
}

// ===========================================================
// Result screen buttons
// ===========================================================

continueButton.addEventListener("click", () => {
  startGame(currentWordLength);
});

changeDifficultyButton.addEventListener("click", () => {
  showScreen(setupScreen);
});

quitButton.addEventListener("click", () => {
  currentGameId = null;
  isAcceptingInput = false;
  showScreen(setupScreen);
});