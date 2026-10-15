# TODO

Planned work and a running recap of what's been done, so a session can pick
up context quickly without re-deriving it from git log.

## Open

### Word Game (web)
- [ ] UI polish — layout, spacing, visual design pass
- [ ] Spanish games are unwinnable in practice: the target word comes from the
      API in Spanish, but guesses are validated against the English-only
      `words.txt` (e.g. "perro", "gato", "casa" are all rejected). Needs a
      Spanish word list, or skip validation for `es`. MobileGames leaves the
      language option out until this is fixed.

### Word Game (mobile — `MobileGames`, .NET MAUI)
- [ ] First run on a real device / simulator — never built for Android or iOS
      yet (the cloud session can't download the Android SDK); only the
      `net10.0` compile and view-model behavior have been verified
- [ ] App icon and splash screen are still the MAUI template's (.NET logo,
      purple `#512BD4`)
- [ ] Physical keyboard input (e.g. Bluetooth keyboard / Mac Catalyst) — only
      the on-screen keyboard works today
- [ ] Persist wins/losses across launches (in memory only, same as web)

### New platforms (not started)
- [ ] `LocalGames` (desktop, offline-first — needs local storage that syncs
      when a connection is available)

### Accounts & social (after MVP polish)
- [ ] Auth — Guest vs. registered `AppUser`, guest → registered upgrade
      without losing in-session progress
- [ ] Persistence — accounts, stats, game history
- [ ] Friends
- [ ] Stats (win rate, streaks, games played)
- [ ] Sharing (results, achievements, invites)

## Recap

### 2026-10-15
- Target-word fallback list (`DictionaryWordProvider.FallbackWords`) now covers
  every supported length, 3-15 (was 3-8; 9-15 letter games silently got a
  shorter word when `random-word-api.herokuapp.com` was unreachable). New words
  are common and all present in the bundled `words.txt`, so they're always
  guessable. Verified by running the app with the API unreachable and winning a
  game at each length 9-15 with a fallback word.
- Started `MobileGames`: a .NET MAUI app (Android, iOS, Mac Catalyst) for the
  Word Game. Game logic runs on the device through `GamesCore`'s
  `WordGameService`, so it plays offline (target word falls back to the bundled
  list without a connection). Mirrors the web game: word length 3-15,
  Easy/Medium/Hard, per-letter tile colors, on-screen keyboard with best-result
  key colors, wins/losses record, light/dark theme with a toolbar toggle that
  persists. Tiles and keys size to the screen width so a 15-letter row fits a
  phone. Result is shown under the final board instead of a separate screen.
  - Verified: compiled against MAUI's plain `net10.0` target (XAML source-gen
    checks every binding — 0 warnings), and a scratch harness drove
    `WordGameViewModel` through typing, deletion, invalid guesses, scoring,
    keyboard colors, win, loss, play-again, and 15-letter layout (24/24 pass).
  - Not verified: an actual Android/iOS build or run (see Open).

### 2026-09-09
Word Game (web) MVP polish pass:
- Removed dead commented-out HTML on the game screen (word length / attempts labels)
- Difficulty input is now an Easy/Medium/Hard select instead of a raw 1-5 number
- Added a clickable on-screen keyboard (Enter/Backspace/letters) alongside physical typing
- Fixed the on-screen keyboard not showing green/yellow feedback for correct/present letters (only "absent" ever had a color)
- Added light/dark theming — OS-preference aware, plus a manual toggle button (top right) that persists via `localStorage`
- Fixed the toggle button retaining keyboard focus and swallowing a follow-up Enter press
- **Guess validation latency**: was calling `dictionaryapi.dev` on every guess, which turned out to be down/flaky (Cloudflare 522s, multi-second hangs). Traced with direct `curl` timing before touching code.
  - First pass: fixed the fail-open logic (a non-2xx response was being treated as "not a real word" instead of "can't verify"), added a 2s `HttpClient` timeout, added a bundled local word-list fallback for outages
  - Final state: dropped the external API from guess validation entirely — guesses are checked against a bundled word list (`GamesCore/Resources/words.txt`, ~211k words filtered from macOS's `/usr/share/dict/words`, embedded resource, loaded once into a `HashSet`). Guess submission is now single-digit ms with no network call.
  - `random-word-api.herokuapp.com` is still used once per game to pick the *target* word — untouched, has its own small local fallback (see Open above for its length-coverage gap)
- Updated README to match all of the above (repo structure, API docs, roadmap checkboxes) and marked "Users & Accounts" as planned/not-yet-implemented rather than current behavior
