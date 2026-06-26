# Games

A monorepo for a collection of small games, playable across multiple
platforms from shared game logic.

## Repo Structure

```
Games/
├── GamesCore/           # Shared game logic — platform-agnostic, no UI/hosting code
│   ├── WordGame/        #   e.g. word selection, guess scoring
│   ├── Users/           #   AppUser, Guest, Friends, Stats — shared across all platforms
│   └── ...
├── WebGames/            # ASP.NET Core web app — hosts the games in-browser
├── MobileGames/         # Mobile app (TBD) — same games, mobile-formatted UI
├── LocalGames/          # Downloadable desktop app (TBD) — playable offline
└── README.md
```

**Why a shared core:** the rules of a game (how a guess is scored, how a
match ends, how a high score is computed) shouldn't be reimplemented per
platform. `GamesCore` holds that logic as plain C# with no dependency on
ASP.NET, a specific UI framework, or a database. `WebGames`, `MobileGames`,
and `LocalGames` each reference `GamesCore` and are responsible only for:
hosting/serving, presentation (HTML/JS, mobile UI, desktop UI), and
platform-specific concerns (auth cookies on web, local storage on desktop,
push notifications on mobile, etc.).

`LocalGames` additionally needs an offline-first data story (e.g. local
SQLite) that syncs to the server when a connection is available, so
accounts/stats stay consistent across platforms.

## Games

| Game | Status | Description |
|------|--------|--------------|
| Word Game | 🚧 In progress | A Wordle-style word-guessing game. Pick a word length (3-15 letters), guess within `length + 1` attempts, get per-letter feedback (correct / present / absent). |

More games to come.

## Users & Accounts

Every session has an `AppUser` — either:
- **Guest** — no account, generic guest identity, play without signing in. Progress/stats aren't persisted across sessions.
- **Registered** — signed-in account with persistent identity (`Username`, `Email`, `DisplayName`, etc.)

Planned features for registered users:
- **Friends** — add/follow other users
- **Persistent high scores** — per-game leaderboards tied to the account
- **Account statistics** — win rate, streaks, games played, etc.
- **Sharing** — share results, achievements, or game invites with others

Guest accounts should be able to upgrade to a registered account without
losing in-session progress (e.g. a guest finishes a game, then signs up —
that result should attach to the new account rather than disappear).

## WebGames — Running locally

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (targeting
`net10.0`).

```bash
cd WebGames
dotnet run
```

The app starts at the URLs configured in `Properties/launchSettings.json`
(`http://localhost:5013` / `https://localhost:7056` by default).

## Word Game API

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/games` | Start a new game. Body: `{ "wordLength": 5 }` (3-15) |
| `POST` | `/api/games/{id}/guess` | Submit a guess. Body: `{ "guess": "apple" }` |
| `GET`  | `/api/games/{id}` | Get current game state |

Word length must be between 3 and 15 letters. The number of allowed
attempts is always `wordLength + 1`.

Word selection and guess validation use a public dictionary API, with a
small local fallback word list if that API is unreachable.

## Roadmap

- [ ] Extract shared game logic into `GamesCore`
- [ ] Word Game frontend (web)
- [ ] User accounts / auth, Guest → registered upgrade path
- [ ] Friends, high scores, stats, sharing
- [ ] `MobileGames` app
- [ ] `LocalGames` desktop app with offline support
- [ ] Additional games (TBD)

## License

MIT — see [LICENSE](LICENSE).