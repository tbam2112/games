# Games

A monorepo for a collection of small games, playable across multiple
platforms from shared game logic.

## Repo Structure

```
Games/
├── GamesCore/           # Shared game logic — platform-agnostic, no UI/hosting code
│   ├── Models/          #   Game, AppUser, etc.
│   ├── Services/        #   WordGameService (rules/scoring), word providers
│   ├── Resources/       #   bundled data, e.g. the local word list
│   └── ...
├── WebGames/            # ASP.NET Core web app — hosts the games in-browser
├── MobileGames/         # .NET MAUI app (Android/iOS/Mac Catalyst) — same games, mobile UI, offline
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
| Word Game | 🚧 In progress (web, mobile) | A Wordle-style word-guessing game. Pick a word length (3-15 letters), guess within `length + 1` attempts, get per-letter feedback (correct / present / absent). |

More games to come.

## Users & Accounts

**Not implemented yet** — games today are anonymous (tracked only by a
per-game GUID, no session or sign-in). `GamesCore.Models.AppUser` exists as
a data model but isn't wired into anything. The intended design:

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

## MobileGames — Running locally

A .NET MAUI app that runs the game logic on the device through `GamesCore`
(no server needed, playable offline). Requires the .NET SDK plus the MAUI
workload, and the platform toolchain for whatever you're targeting — Xcode
for iOS / Mac Catalyst, the Android SDK for Android:

```bash
dotnet workload install maui
cd MobileGames
dotnet build -t:Run -f net10.0-ios            # iOS simulator (macOS only)
dotnet build -t:Run -f net10.0-maccatalyst    # as a Mac app (macOS only)
dotnet build -t:Run -f net10.0-android        # Android emulator/device
```

Or open `Games.slnx` in Visual Studio / Rider and run the `MobileGames`
project. Note that building the whole solution needs the MAUI workload
installed; `WebGames` alone still builds without it (`cd WebGames && dotnet run`).

The mobile Word Game supports word length 3-15 and Easy/Medium/Hard, with an
on-screen keyboard and light/dark theme. English only for now (see
`TODO.md` — Spanish guess validation isn't working yet).

## Word Game API

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/games` | Start a new game. Body: `{ "wordLength": 5, "difficulty": 1, "language": "en" }` — `wordLength` 3-15, `difficulty` 1 (common words) to 5 (rare words), `language` `"en"`/`"es"` |
| `POST` | `/api/games/{id}/guess` | Submit a guess. Body: `{ "guess": "apple" }` |
| `GET`  | `/api/games/{id}` | Get current game state |

Word length must be between 3 and 15 letters. The number of allowed
attempts is always `wordLength + 1`.

Guess validation checks against a bundled local word list (no network
call). Target word selection calls a public random-word API, falling back
to a small local word list if that API is unreachable.

## Roadmap

- [x] Extract shared game logic into `GamesCore`
- [x] Word Game frontend (web)
- [ ] User accounts / auth, Guest → registered upgrade path
- [ ] Friends, high scores, stats, sharing
- [ ] `MobileGames` app — Word Game started (.NET MAUI), not yet run on a device
- [ ] `LocalGames` desktop app with offline support
- [ ] Additional games (TBD)

## License

MIT — see [LICENSE](LICENSE).