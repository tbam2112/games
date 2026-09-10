# TODO

Planned work and a running recap of what's been done, so a session can pick
up context quickly without re-deriving it from git log.

## Open

### Word Game (web)
- [ ] UI polish — layout, spacing, visual design pass
- [ ] `GetRandomWordAsync`'s fallback list (`DictionaryWordProvider.FallbackWords`)
      only covers lengths 3-8; longer games silently fall back to the closest
      length instead of the requested one. Guess *validation* already covers
      3-15 via the bundled word list — this is only the target-word-selection
      fallback used when `random-word-api.herokuapp.com` is unreachable.
- [ ] README is stale in a couple of spots: "Repo Structure" describes
      `GamesCore/WordGame/` and `GamesCore/Users/`, but the actual layout is
      `GamesCore/Models/` and `GamesCore/Services/`. The "Word Game API"
      section also still says guess validation uses a public dictionary API —
      it's fully local now (see recap below).

### New platforms (not started)
- [ ] `MobileGames`
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
